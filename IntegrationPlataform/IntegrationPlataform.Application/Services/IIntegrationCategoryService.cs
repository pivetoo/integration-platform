using Archon.Core.Pagination;
using Archon.Application.Services;
using IntegrationPlataform.Application.Requests.IntegrationCategories;
using IntegrationPlataform.Domain.Entities;

namespace IntegrationPlataform.Application.Services
{
    public interface IIntegrationCategoryService : ICrudService<IntegrationCategory>
    {
        Task<PagedResult<IntegrationCategory>> GetIntegrationCategories(PagedRequest request, CancellationToken cancellationToken = default);

        Task<IntegrationCategory?> GetIntegrationCategoryById(long id, CancellationToken cancellationToken = default);

        Task<List<IntegrationCategory>> GetActiveIntegrationCategories(CancellationToken cancellationToken = default);

        Task<IntegrationCategory> CreateIntegrationCategory(CreateIntegrationCategoryRequest request, CancellationToken cancellationToken = default);

        Task<IntegrationCategory> UpdateIntegrationCategory(long id, UpdateIntegrationCategoryRequest request, CancellationToken cancellationToken = default);
    }
}
