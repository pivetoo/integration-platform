using Archon.Application.Services;
using IntegrationPlataform.Application.Requests.Pipelines;
using IntegrationPlataform.Domain.Entities;

namespace IntegrationPlataform.Application.Services
{
    public interface IPipelineService : ICrudService<Pipeline>
    {
        Task<Pipeline> CreatePipeline(CreatePipelineRequest request, CancellationToken cancellationToken = default);

        Task<Pipeline> UpdatePipeline(long id, UpdatePipelineRequest request, CancellationToken cancellationToken = default);
    }
}
