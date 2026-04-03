using IntegrationPlataform.Domain.Entities;

namespace IntegrationPlataform.Application.Services
{
    public interface IQueueProcessorService
    {
        Task<IReadOnlyCollection<ProcessingQueue>> GetPendingToProcess(CancellationToken cancellationToken = default);

        Task ProcessItem(long processingQueueId, CancellationToken cancellationToken = default);
    }
}
