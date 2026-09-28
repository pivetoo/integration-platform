using IntegrationPlatform.Domain.Entities;

namespace IntegrationPlatform.Application.Services
{
    public interface IServiceExecutionService
    {
        Task<Execution> ExecuteService(string serviceIdentifier, long connectorId, string? inputData, CancellationToken cancellationToken = default);

        Task<ProcessingQueue> EnqueueService(string serviceIdentifier, long connectorId, string? inputData, int priority = 5, DateTime? scheduledFor = null, string? idempotencyKey = null, CancellationToken cancellationToken = default);
    }
}
