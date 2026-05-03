using IntegrationPlatform.Domain.Entities;

namespace IntegrationPlatform.Application.Services
{
    public interface IQueueProcessorService
    {
        Task<IReadOnlyCollection<ProcessingQueue>> GetPendingToProcess(CancellationToken cancellationToken = default);

        Task ProcessItem(long processingQueueId, CancellationToken cancellationToken = default);
    }
}
