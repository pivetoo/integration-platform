using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;

namespace IntegrationPlataform.Infrastructure.Services
{
    public sealed class QueueProcessorService : IQueueProcessorService
    {
        public Task<IReadOnlyCollection<ProcessingQueue>> GetPendingToProcess(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Queue processing migration is still pending.");
        }

        public Task ProcessItem(long processingQueueId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Queue processing migration is still pending.");
        }
    }
}
