using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using IntegrationPlatform.Domain.ValueObjects;
using IntegrationPlatform.Infrastructure.BackgroundJobs;
using IntegrationPlatform.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;

namespace IntegrationPlatform.Infrastructure.Services.ExecutionEngine
{
    // Entrega HTTP das callbacks do outbox, com retry/backoff exponencial. Distingue falha permanente
    // (4xx -> Failed) de transitoria (timeout/5xx -> reagenda ate CallbackMaxAttempts).
    public sealed class CallbackDeliveryService : ICallbackDeliveryService
    {
        private readonly DbContext dbContext;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly ILogger<CallbackDeliveryService> logger;
        private readonly BackgroundJobOptions options;

        public CallbackDeliveryService(DbContext dbContext, IHttpClientFactory httpClientFactory, ILogger<CallbackDeliveryService> logger, IOptions<BackgroundJobOptions> options)
        {
            this.dbContext = dbContext;
            this.httpClientFactory = httpClientFactory;
            this.logger = logger;
            this.options = options.Value;
        }

        public async Task<IReadOnlyCollection<CallbackDelivery>> GetPendingToDeliver(int maxItems, CancellationToken cancellationToken = default)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            return await dbContext.Set<CallbackDelivery>()
                .AsNoTracking()
                .Where(item => item.Status == CallbackDeliveryStatus.Pending
                    && (item.NextAttemptAt == null || item.NextAttemptAt <= now))
                .OrderBy(item => item.NextAttemptAt)
                .Take(maxItems)
                .ToListAsync(cancellationToken);
        }

        public async Task DeliverItem(long callbackDeliveryId, CancellationToken cancellationToken = default)
        {
            CallbackDelivery? delivery = await dbContext.Set<CallbackDelivery>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == callbackDeliveryId, cancellationToken);

            if (delivery is null || delivery.Status != CallbackDeliveryStatus.Pending)
            {
                return;
            }

            DateTimeOffset now = DateTimeOffset.UtcNow;

            if (!await OutboundUrlGuard.IsAllowedAsync(delivery.CallbackUrl, cancellationToken))
            {
                delivery.MarkFailed("CallbackUrl bloqueada (protecao SSRF).");
                delivery.SetUpdatedAt(now);
                await dbContext.SaveChangesAsync(cancellationToken);
                logger.LogWarning("Callback delivery {Id} blocked: CallbackUrl is not allowed (SSRF protection).", delivery.Id);
                return;
            }

            try
            {
                HttpClient client = httpClientFactory.CreateClient("outbound");
                client.Timeout = TimeSpan.FromSeconds(15);

                using HttpRequestMessage request = new(HttpMethod.Post, delivery.CallbackUrl)
                {
                    Content = new StringContent(delivery.Payload, Encoding.UTF8, "application/json")
                };

                if (!string.IsNullOrWhiteSpace(delivery.CallbackToken))
                {
                    request.Headers.TryAddWithoutValidation("X-Callback-Token", delivery.CallbackToken);
                }

                request.Headers.UserAgent.Add(new ProductInfoHeaderValue("IntegrationPlatform", "1.0"));

                using HttpResponseMessage response = await client.SendAsync(request, cancellationToken);

                int statusCode = (int)response.StatusCode;
                if (response.IsSuccessStatusCode)
                {
                    delivery.MarkDelivered(now);
                }
                else if (statusCode is >= 400 and < 500)
                {
                    delivery.MarkFailed($"HTTP {statusCode}");
                }
                else
                {
                    ScheduleRetryOrFail(delivery, now, $"HTTP {statusCode}");
                }
            }
            catch (Exception exception)
            {
                ScheduleRetryOrFail(delivery, now, exception.Message);
            }

            delivery.SetUpdatedAt(DateTimeOffset.UtcNow);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private void ScheduleRetryOrFail(CallbackDelivery delivery, DateTimeOffset now, string error)
        {
            TimeSpan? delay = CallbackRetryPolicy.NextRetryDelay(delivery.Attempts, options.CallbackMaxAttempts, options.CallbackBaseBackoffSeconds);
            if (delay is null)
            {
                delivery.MarkFailed(error);
                logger.LogWarning("Callback delivery {Id} permanently failed after {Attempts} attempt(s): {Error}", delivery.Id, delivery.Attempts + 1, error);
                return;
            }

            delivery.ScheduleRetry(now + delay.Value, error);
        }
    }
}
