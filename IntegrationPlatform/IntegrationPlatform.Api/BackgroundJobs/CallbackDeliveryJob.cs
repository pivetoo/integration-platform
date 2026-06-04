using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using IntegrationPlatform.Infrastructure.BackgroundJobs;
using Microsoft.Extensions.Options;

namespace IntegrationPlatform.Api.BackgroundJobs
{
    // Reprocessa o outbox de callbacks: por tenant, entrega as CallbackDelivery pendentes cujo
    // NextAttemptAt ja chegou. A entrega/retry/backoff e responsabilidade do CallbackDeliveryService.
    public sealed class CallbackDeliveryJob : BackgroundService
    {
        private readonly TenantJobRunner tenantJobRunner;
        private readonly ILogger<CallbackDeliveryJob> logger;
        private readonly BackgroundJobOptions options;

        public CallbackDeliveryJob(TenantJobRunner tenantJobRunner, ILogger<CallbackDeliveryJob> logger, IOptions<BackgroundJobOptions> options)
        {
            this.tenantJobRunner = tenantJobRunner;
            this.logger = logger;
            this.options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!options.CallbackDeliveryEnabled)
            {
                logger.LogInformation("Callback delivery worker is disabled.");
                return;
            }

            logger.LogInformation("Callback delivery worker started. Polling every {Interval}.", options.CallbackDeliveryPollingInterval);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await DeliverPending(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Unexpected error while delivering callbacks.");
                }

                try
                {
                    await Task.Delay(options.CallbackDeliveryPollingInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            logger.LogInformation("Callback delivery worker stopped.");
        }

        private async Task DeliverPending(CancellationToken cancellationToken)
        {
            await tenantJobRunner.RunForAllTenants(async (provider, tenantCancellationToken) =>
            {
                ICallbackDeliveryService callbackDeliveryService = provider.GetRequiredService<ICallbackDeliveryService>();
                IReadOnlyCollection<CallbackDelivery> pending = await callbackDeliveryService.GetPendingToDeliver(options.MaxItemsPerCycle, tenantCancellationToken);

                foreach (CallbackDelivery delivery in pending)
                {
                    tenantCancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        await callbackDeliveryService.DeliverItem(delivery.Id, tenantCancellationToken);
                    }
                    catch (Exception exception)
                    {
                        logger.LogError(exception, "Failed to deliver callback {CallbackDeliveryId}.", delivery.Id);
                    }
                }
            }, cancellationToken);
        }
    }
}
