using Archon.Application.Services;
using Archon.Core.Pagination;
using IntegrationPlataform.Application.Requests.PipelineStepValueMappings;
using IntegrationPlataform.Domain.Entities;

namespace IntegrationPlataform.Application.Services
{
    public interface IPipelineStepValueMappingService : ICrudService<PipelineStepValueMapping>
    {
        Task<PagedResult<PipelineStepValueMapping>> GetPipelineStepValueMappings(PagedRequest request, CancellationToken cancellationToken = default);

        Task<PipelineStepValueMapping?> GetPipelineStepValueMappingById(long id, CancellationToken cancellationToken = default);

        Task<List<PipelineStepValueMapping>> GetByPipelineStep(long pipelineStepId, CancellationToken cancellationToken = default);

        Task<PipelineStepValueMapping> CreatePipelineStepValueMapping(CreatePipelineStepValueMappingRequest request, CancellationToken cancellationToken = default);

        Task<PipelineStepValueMapping> UpdatePipelineStepValueMapping(long id, UpdatePipelineStepValueMappingRequest request, CancellationToken cancellationToken = default);
    }
}
