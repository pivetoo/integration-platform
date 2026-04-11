using Archon.Application.Services;
using Archon.Core.Pagination;
using IntegrationPlataform.Application.Requests.PipelineStepExamples;
using IntegrationPlataform.Domain.Entities;

namespace IntegrationPlataform.Application.Services
{
    public interface IPipelineStepExampleService : ICrudService<PipelineStepExample>
    {
        Task<PagedResult<PipelineStepExample>> GetPipelineStepExamples(PagedRequest request, CancellationToken cancellationToken = default);

        Task<PipelineStepExample?> GetPipelineStepExampleById(long id, CancellationToken cancellationToken = default);

        Task<List<PipelineStepExample>> GetByPipelineStep(long pipelineStepId, CancellationToken cancellationToken = default);

        Task<PipelineStepExample> CreatePipelineStepExample(CreatePipelineStepExampleRequest request, CancellationToken cancellationToken = default);

        Task<PipelineStepExample> UpdatePipelineStepExample(long id, UpdatePipelineStepExampleRequest request, CancellationToken cancellationToken = default);
    }
}
