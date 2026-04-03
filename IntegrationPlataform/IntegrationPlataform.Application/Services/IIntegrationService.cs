using Archon.Application.Services;
using IntegrationPlataform.Application.Requests.Integrations;
using IntegrationPlataform.Domain.Entities;

namespace IntegrationPlataform.Application.Services
{
    public interface IIntegrationService : ICrudService<Integration>
    {
        Task<Integration> CreateIntegration(CreateIntegrationRequest request, CancellationToken cancellationToken = default);

        Task<Integration> UpdateIntegration(long id, UpdateIntegrationRequest request, CancellationToken cancellationToken = default);
    }
}
