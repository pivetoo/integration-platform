using Archon.Core.Pagination;
using Archon.Application.Services;
using IntegrationPlatform.Domain.Entities;
using IntegrationPlatform.Application.Requests.PipelineRoutines;

namespace IntegrationPlatform.Application.Services
{
    public interface IPipelineRoutineService : ICrudService<PipelineRoutine>
    {
        Task<PagedResult<PipelineRoutine>> GetPipelineRoutines(PagedRequest request, CancellationToken cancellationToken = default);

        Task<PipelineRoutine?> GetPipelineRoutineById(long id, CancellationToken cancellationToken = default);

        Task<PipelineRoutine> CreatePipelineRoutine(CreatePipelineRoutineRequest request, CancellationToken cancellationToken = default);

        Task<PipelineRoutine> UpdatePipelineRoutine(
            long id,
            int intervalMinutes,
            bool isActive,
            string? defaultPayload,
            DateTimeOffset? nextExecution,
            CancellationToken cancellationToken = default);
    }
}
