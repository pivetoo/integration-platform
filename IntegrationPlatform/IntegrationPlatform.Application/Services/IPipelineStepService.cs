using Archon.Core.Pagination;
using Archon.Application.Services;
using IntegrationPlatform.Application.Requests.PipelineSteps;
using IntegrationPlatform.Domain.Entities;

namespace IntegrationPlatform.Application.Services
{
    public interface IPipelineStepService : ICrudService<PipelineStep>
    {
        Task<PagedResult<PipelineStep>> GetPipelineSteps(PagedRequest request, CancellationToken cancellationToken = default);

        Task<PipelineStep?> GetPipelineStepById(long id, CancellationToken cancellationToken = default);

        Task<List<PipelineStep>> GetPipelineStepsByPipeline(long pipelineId, CancellationToken cancellationToken = default);

        Task<PipelineStep> CreatePipelineStep(CreatePipelineStepRequest request, CancellationToken cancellationToken = default);

        Task<PipelineStep> UpdatePipelineStep(long id, UpdatePipelineStepRequest request, CancellationToken cancellationToken = default);
    }
}
