using Archon.Core.Pagination;
using Archon.Application.Services;
using IntegrationPlatform.Application.Requests.IntegrationCategories;
using IntegrationPlatform.Domain.Entities;

namespace IntegrationPlatform.Application.Services
{
    public interface IIntegrationCategoryService : ICrudService<IntegrationCategory>
    {
        Task<PagedResult<IntegrationCategory>> GetIntegrationCategories(PagedRequest request, string? search, CancellationToken cancellationToken = default);

        Task<IntegrationCategory?> GetIntegrationCategoryById(long id, CancellationToken cancellationToken = default);

        Task<IntegrationCategory?> GetIntegrationCategoryByIdentifier(string identifier, CancellationToken cancellationToken = default);

        Task<List<IntegrationCategory>> GetActiveIntegrationCategories(CancellationToken cancellationToken = default);

        Task<IntegrationCategory> CreateIntegrationCategory(CreateIntegrationCategoryRequest request, CancellationToken cancellationToken = default);

        Task<IntegrationCategory> UpdateIntegrationCategory(long id, UpdateIntegrationCategoryRequest request, CancellationToken cancellationToken = default);
    }
}
