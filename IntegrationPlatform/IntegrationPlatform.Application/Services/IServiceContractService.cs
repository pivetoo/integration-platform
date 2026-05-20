using Archon.Application.Services;
using Archon.Core.Pagination;
using IntegrationPlatform.Application.Requests.ServiceContracts;
using IntegrationPlatform.Domain.Entities;

namespace IntegrationPlatform.Application.Services
{
    public interface IServiceContractService : ICrudService<ServiceContract>
    {
        Task<PagedResult<ServiceContract>> GetServiceContracts(PagedRequest request, string? search, long? integrationCategoryId, CancellationToken cancellationToken = default);

        Task<ServiceContract?> GetServiceContractById(long id, CancellationToken cancellationToken = default);

        Task<ServiceContract?> GetServiceContractByIdentifier(string identifier, CancellationToken cancellationToken = default);

        Task<List<ServiceContract>> GetActiveServiceContracts(CancellationToken cancellationToken = default);

        Task<List<ServiceContract>> GetServiceContractsByIntegration(long integrationId, CancellationToken cancellationToken = default);

        Task<ServiceContract> CreateServiceContract(CreateServiceContractRequest request, CancellationToken cancellationToken = default);

        Task<ServiceContract> UpdateServiceContract(long id, UpdateServiceContractRequest request, CancellationToken cancellationToken = default);

        Task<IntegrationServiceContract> SetIntegrationServiceContract(SetIntegrationServiceContractRequest request, CancellationToken cancellationToken = default);

        Task<bool> RemoveIntegrationServiceContract(long integrationId, long serviceContractId, CancellationToken cancellationToken = default);
    }
}
