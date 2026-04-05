using Archon.Core.Pagination;
using Archon.Application.Services;
using IntegrationPlataform.Domain.Entities;
using IntegrationPlataform.Application.Requests.References;

namespace IntegrationPlataform.Application.Services
{
    public interface IReferenceService : ICrudService<Reference>
    {
        Task<PagedResult<Reference>> GetReferences(PagedRequest request, CancellationToken cancellationToken = default);

        Task<Reference?> GetReferenceById(long id, CancellationToken cancellationToken = default);

        Task<List<Reference>> GetReferencesByConnector(long connectorId, CancellationToken cancellationToken = default);

        Task<Reference> CreateReference(CreateReferenceRequest request, CancellationToken cancellationToken = default);

        Task<Reference> UpdateReference(long id, UpdateReferenceRequest request, CancellationToken cancellationToken = default);
    }
}
