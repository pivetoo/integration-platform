using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using IntegrationPlatform.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlatform.Infrastructure.Services.ExecutionEngine
{
    // Enfileira o callback de uma execucao Success/Partial num outbox (CallbackDelivery). A entrega
    // HTTP e a reentrega com retry/backoff ficam a cargo do CallbackDeliveryService/CallbackDeliveryJob,
    // garantindo entrega duravel mesmo que o consumidor esteja indisponivel ou o processo reinicie.
    public sealed class ServiceCallbackDispatcher : IServiceCallbackDispatcher
    {
        private readonly DbContext dbContext;

        public ServiceCallbackDispatcher(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<ServiceCallbackResult> DispatchAsync(Execution execution, CancellationToken cancellationToken = default)
        {
            if (execution.Status != ExecutionStatus.Success && execution.Status != ExecutionStatus.Partial)
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

            DateTimeOffset now = DateTimeOffset.UtcNow;
            CallbackDelivery delivery = new(
                execution.Id,
                connector.Id,
                pipeline.ServiceContract.Identifier,
                connector.CallbackUrl!,
                connector.CallbackToken,
                body,
                now);
            delivery.SetCreatedAt(now);

            dbContext.Set<CallbackDelivery>().Add(delivery);
            await dbContext.SaveChangesAsync(cancellationToken);

            return new ServiceCallbackResult(true, true, "callback.enqueued");
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
