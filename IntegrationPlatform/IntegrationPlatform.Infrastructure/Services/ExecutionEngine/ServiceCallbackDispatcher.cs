using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using IntegrationPlatform.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text;

namespace IntegrationPlatform.Infrastructure.Services.ExecutionEngine
{
    public sealed class ServiceCallbackDispatcher : IServiceCallbackDispatcher
    {
        private readonly DbContext dbContext;
        private readonly IHttpClientFactory httpClientFactory;

        public ServiceCallbackDispatcher(DbContext dbContext, IHttpClientFactory httpClientFactory)
        {
            this.dbContext = dbContext;
            this.httpClientFactory = httpClientFactory;
        }

        public async Task<ServiceCallbackResult> DispatchAsync(Execution execution, CancellationToken cancellationToken = default)
        {
            if (execution.Status != ExecutionStatus.Success)
            {
                return new ServiceCallbackResult(false, false, "execution.notSuccessful");
            }

            if (!execution.PipelineId.HasValue || execution.ConnectorId <= 0)
            {
                return new ServiceCallbackResult(false, false, "execution.missingReferences");
            }

            Pipeline? pipeline = await dbContext.Set<Pipeline>()
                .AsNoTracking()
                .Include(item => item.ServiceContract)
                .FirstOrDefaultAsync(item => item.Id == execution.PipelineId.Value, cancellationToken);

            if (pipeline?.ServiceContract is null)
            {
                return new ServiceCallbackResult(false, false, "pipeline.noServiceContract");
            }

            if (!pipeline.ServiceContract.HasCallback)
            {
                return new ServiceCallbackResult(false, false, "serviceContract.noCallback");
            }

            Connector? connector = await dbContext.Set<Connector>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == execution.ConnectorId, cancellationToken);

            if (connector is null || string.IsNullOrWhiteSpace(connector.CallbackUrl))
            {
                return new ServiceCallbackResult(false, false, "connector.noCallbackUrl");
            }

            string body = BuildCallbackBody(execution, pipeline.ServiceContract, connector);

            HttpClient client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(15);

            using HttpRequestMessage request = new(HttpMethod.Post, connector.CallbackUrl)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };

            if (!string.IsNullOrWhiteSpace(connector.CallbackToken))
            {
                request.Headers.TryAddWithoutValidation("X-Callback-Token", connector.CallbackToken);
            }

            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("IntegrationPlatform", "1.0"));

            try
            {
                using HttpResponseMessage response = await client.SendAsync(request, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return new ServiceCallbackResult(true, true, $"HTTP {(int)response.StatusCode}");
                }

                return new ServiceCallbackResult(true, false, $"HTTP {(int)response.StatusCode}");
            }
            catch (Exception exception)
            {
                return new ServiceCallbackResult(true, false, exception.Message);
            }
        }

        private static string BuildCallbackBody(Execution execution, ServiceContract service, Connector connector)
        {
            var envelope = new
            {
                serviceIdentifier = service.Identifier,
                connectorId = connector.Id,
                connectorName = connector.Name,
                executionId = execution.Id,
                status = execution.Status.ToString(),
                startedAt = execution.StartedAt,
                finishedAt = execution.FinishedAt,
                output = TryParseJson(execution.OutputData)
            };

            return System.Text.Json.JsonSerializer.Serialize(envelope);
        }

        private static object? TryParseJson(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<object>(raw);
            }
            catch
            {
                return raw;
            }
        }
    }
}
