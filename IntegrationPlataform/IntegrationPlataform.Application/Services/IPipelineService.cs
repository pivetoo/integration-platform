using Archon.Core.Pagination;
using Archon.Application.Services;
using IntegrationPlataform.Application.Requests.Pipelines;
using IntegrationPlataform.Domain.Entities;

namespace IntegrationPlataform.Application.Services
{
    public interface IPipelineService : ICrudService<Pipeline>
    {
        Task<PagedResult<Pipeline>> GetPipelines(PagedRequest request, CancellationToken cancellationToken = default);

        Task<Pipeline?> GetPipelineById(long id, CancellationToken cancellationToken = default);

        Task<List<Pipeline>> GetPipelinesByIntegration(long integrationId, CancellationToken cancellationToken = default);

        Task<List<Pipeline>> GetActivePipelines(CancellationToken cancellationToken = default);

        Task<Pipeline> CreatePipeline(CreatePipelineRequest request, CancellationToken cancellationToken = default);

        Task<Pipeline> UpdatePipeline(long id, UpdatePipelineRequest request, CancellationToken cancellationToken = default);
    }
}
