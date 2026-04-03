using Archon.Application.Services;
using IntegrationPlataform.Application.Requests.PipelineSteps;
using IntegrationPlataform.Domain.Entities;

namespace IntegrationPlataform.Application.Services
{
    public interface IPipelineStepService : ICrudService<PipelineStep>
    {
        Task<PipelineStep> CreatePipelineStep(CreatePipelineStepRequest request, CancellationToken cancellationToken = default);

        Task<PipelineStep> UpdatePipelineStep(long id, UpdatePipelineStepRequest request, CancellationToken cancellationToken = default);
    }
}
