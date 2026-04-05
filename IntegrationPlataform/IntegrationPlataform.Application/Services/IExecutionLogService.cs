using Archon.Core.Pagination;
using Archon.Application.Services;
using IntegrationPlataform.Domain.Entities;

namespace IntegrationPlataform.Application.Services
{
    public interface IExecutionLogService : ICrudService<ExecutionLog>
    {
        Task<PagedResult<ExecutionLog>> GetExecutionLogs(PagedRequest request, CancellationToken cancellationToken = default);

        Task<ExecutionLog?> GetExecutionLogById(long id, CancellationToken cancellationToken = default);

        Task<List<ExecutionLog>> GetExecutionLogsByExecution(long executionId, CancellationToken cancellationToken = default);
    }
}
