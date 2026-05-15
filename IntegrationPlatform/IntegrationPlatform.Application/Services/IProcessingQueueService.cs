using Archon.Core.Pagination;
using Archon.Application.Services;
using IntegrationPlatform.Domain.Entities;

namespace IntegrationPlatform.Application.Services
{
    public interface IProcessingQueueService : ICrudService<ProcessingQueue>
    {
        Task<PagedResult<ProcessingQueue>> GetProcessingQueues(PagedRequest request, string? search, CancellationToken cancellationToken = default);

        Task<ProcessingQueue?> GetProcessingQueueById(long id, CancellationToken cancellationToken = default);

        Task<List<ProcessingQueue>> GetPendingProcessingQueues(CancellationToken cancellationToken = default);
    }
}
