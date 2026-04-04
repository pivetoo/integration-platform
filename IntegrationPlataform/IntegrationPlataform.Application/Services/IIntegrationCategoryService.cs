using Archon.Application.Services;
using IntegrationPlataform.Application.Requests.IntegrationCategories;
using IntegrationPlataform.Domain.Entities;

namespace IntegrationPlataform.Application.Services
{
    public interface IIntegrationCategoryService : ICrudService<IntegrationCategory>
    {
        Task<IntegrationCategory> CreateIntegrationCategory(CreateIntegrationCategoryRequest request, CancellationToken cancellationToken = default);

        Task<IntegrationCategory> UpdateIntegrationCategory(long id, UpdateIntegrationCategoryRequest request, CancellationToken cancellationToken = default);
    }
}
