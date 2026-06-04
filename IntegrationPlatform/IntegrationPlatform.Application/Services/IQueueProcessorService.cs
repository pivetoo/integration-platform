using IntegrationPlatform.Domain.Entities;

namespace IntegrationPlatform.Application.Services
{
    public interface IQueueProcessorService
    {
        Task<IReadOnlyCollection<ProcessingQueue>> GetPendingToProcess(int maxItems, CancellationToken cancellationToken = default);

        Task<int> RecoverStuckItems(TimeSpan timeout, CancellationToken cancellationToken = default);

        Task<int> RecoverStuckExecutions(TimeSpan timeout, CancellationToken cancellationToken = default);

        Task ProcessItem(long processingQueueId, CancellationToken cancellationToken = default);
    }
}
