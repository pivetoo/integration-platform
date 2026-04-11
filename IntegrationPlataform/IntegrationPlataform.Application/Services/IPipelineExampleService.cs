using Archon.Application.Services;
using Archon.Core.Pagination;
using IntegrationPlataform.Application.Requests.PipelineExamples;
using IntegrationPlataform.Domain.Entities;

namespace IntegrationPlataform.Application.Services
{
    public interface IPipelineExampleService : ICrudService<PipelineExample>
    {
        Task<PagedResult<PipelineExample>> GetPipelineExamples(PagedRequest request, CancellationToken cancellationToken = default);

        Task<PipelineExample?> GetPipelineExampleById(long id, CancellationToken cancellationToken = default);

        Task<List<PipelineExample>> GetByPipeline(long pipelineId, CancellationToken cancellationToken = default);

        Task<PipelineExample> CreatePipelineExample(CreatePipelineExampleRequest request, CancellationToken cancellationToken = default);

        Task<PipelineExample> UpdatePipelineExample(long id, UpdatePipelineExampleRequest request, CancellationToken cancellationToken = default);
    }
}
