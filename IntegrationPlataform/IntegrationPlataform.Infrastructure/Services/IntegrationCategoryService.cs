using Archon.Infrastructure.Services;
using IntegrationPlataform.Application.Requests.IntegrationCategories;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Infrastructure.Services
{
    public sealed class IntegrationCategoryService : CrudService<IntegrationCategory>, IIntegrationCategoryService
    {
        public IntegrationCategoryService(DbContext dbContext) : base(dbContext)
        {
        }

        public async Task<IntegrationCategory> CreateIntegrationCategory(CreateIntegrationCategoryRequest request, CancellationToken cancellationToken = default)
        {
            IntegrationCategory category = new(request.Name, request.Description);
            bool success = await Insert(cancellationToken, category);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return category;
        }

        public async Task<IntegrationCategory> UpdateIntegrationCategory(long id, UpdateIntegrationCategoryRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException("Route id does not match body id.");
            }

            IntegrationCategory? category = await DbContext.Set<IntegrationCategory>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (category is null)
            {
                throw new InvalidOperationException("Integration category not found.");
            }

            category.Update(request.Name, request.Description, request.IsActive);

            IntegrationCategory? result = await Update(category, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return result;
        }
    }
}
