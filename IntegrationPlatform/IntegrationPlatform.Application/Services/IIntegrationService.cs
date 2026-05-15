using Archon.Core.Pagination;
using Archon.Application.Services;
using IntegrationPlatform.Application.Requests.Integrations;
using IntegrationPlatform.Domain.Entities;

namespace IntegrationPlatform.Application.Services
{
    public interface IIntegrationService : ICrudService<Integration>
    {
        Task<PagedResult<Integration>> GetIntegrations(PagedRequest request, string? search, CancellationToken cancellationToken = default);

        Task<Integration?> GetIntegrationById(long id, CancellationToken cancellationToken = default);

        Task<List<Integration>> GetActiveIntegrations(CancellationToken cancellationToken = default);

        Task<List<Integration>> GetIntegrationsByCategory(long categoryId, CancellationToken cancellationToken = default);

        Task<Integration> CreateIntegration(CreateIntegrationRequest request, CancellationToken cancellationToken = default);

        Task<Integration> UpdateIntegration(long id, UpdateIntegrationRequest request, CancellationToken cancellationToken = default);
    }
}
