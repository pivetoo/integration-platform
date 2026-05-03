using Archon.Core.Pagination;
using Archon.Application.Services;
using IntegrationPlatform.Domain.Entities;
using IntegrationPlatform.Domain.ValueObjects;

namespace IntegrationPlatform.Application.Services
{
    public interface IExecutionService : ICrudService<Execution>
    {
        Task<PagedResult<Execution>> GetExecutions(PagedRequest request, CancellationToken cancellationToken = default);

        Task<Execution?> GetExecutionById(long id, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<Execution>> GetByConnector(long connectorId, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<Execution>> GetByStatus(ExecutionStatus status, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<Execution>> GetRecent(int take, CancellationToken cancellationToken = default);

        Task<ProcessingQueue> EnqueuePipeline(long connectorId, long pipelineId, string? payload, int priority, CancellationToken cancellationToken = default);
    }
}
