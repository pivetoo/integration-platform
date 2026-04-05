using Archon.Core.Pagination;
using Archon.Application.Services;
using IntegrationPlataform.Domain.Entities;

namespace IntegrationPlataform.Application.Services
{
    public interface IProcessingQueueService : ICrudService<ProcessingQueue>
    {
        Task<PagedResult<ProcessingQueue>> GetProcessingQueues(PagedRequest request, CancellationToken cancellationToken = default);

        Task<ProcessingQueue?> GetProcessingQueueById(long id, CancellationToken cancellationToken = default);

        Task<List<ProcessingQueue>> GetPendingProcessingQueues(CancellationToken cancellationToken = default);
    }
}
