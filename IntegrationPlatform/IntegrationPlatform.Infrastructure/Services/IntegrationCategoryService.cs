using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using IntegrationPlatform.Application.Localization;
using IntegrationPlatform.Application.Requests.IntegrationCategories;
using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace IntegrationPlatform.Infrastructure.Services
{
    public sealed class IntegrationCategoryService : CrudService<IntegrationCategory>, IIntegrationCategoryService
    {
        private readonly IStringLocalizer<IntegrationPlatformResource> Localizer;

        public IntegrationCategoryService(DbContext dbContext, IStringLocalizer<IntegrationPlatformResource> localizer) : base(dbContext)
        {
            Localizer = localizer;
        }

        public async Task<PagedResult<IntegrationCategory>> GetIntegrationCategories(PagedRequest request, string? search, CancellationToken cancellationToken = default)
        {
            var query = DbContext.Set<IntegrationCategory>().AsNoTracking();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var lower = search.ToLower();
                query = query.Where(item => item.Name.ToLower().Contains(lower) || (item.Description != null && item.Description.ToLower().Contains(lower)));
            }
            return await query
                .OrderByDescending(item => item.Id)
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<IntegrationCategory?> GetIntegrationCategoryById(long id, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<IntegrationCategory>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<IntegrationCategory?> GetIntegrationCategoryByIdentifier(string identifier, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return null;
            }

            string normalized = identifier.Trim().ToLower();
            return await DbContext.Set<IntegrationCategory>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Identifier == normalized, cancellationToken);
        }

        public async Task<List<IntegrationCategory>> GetActiveIntegrationCategories(CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<IntegrationCategory>()
                .AsNoTracking()
                .Where(item => item.IsActive)
                .OrderBy(item => item.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<IntegrationCategory> CreateIntegrationCategory(CreateIntegrationCategoryRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureIdentifierIsUnique(request.Identifier, ignoreId: null, cancellationToken);

            IntegrationCategory category = new(request.Identifier, request.Name, request.Description);
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
                throw new InvalidOperationException("request.route.idMismatch");
            }

            IntegrationCategory? category = await DbContext.Set<IntegrationCategory>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (category is null)
            {
                throw new InvalidOperationException("integration.category.notFound");
            }

            await EnsureIdentifierIsUnique(request.Identifier, ignoreId: id, cancellationToken);

            category.Update(request.Identifier, request.Name, request.Description, request.IsActive);

            IntegrationCategory? result = await Update(category, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return result;
        }

        private async Task EnsureIdentifierIsUnique(string identifier, long? ignoreId, CancellationToken cancellationToken)
        {
            string normalized = identifier.Trim().ToLower();

            bool exists = await DbContext.Set<IntegrationCategory>()
                .AsNoTracking()
                .AnyAsync(item => item.Identifier == normalized && (ignoreId == null || item.Id != ignoreId), cancellationToken);

            if (exists)
            {
                throw new InvalidOperationException("integrationCategory.identifier.duplicated");
            }
        }

        public override async Task<IntegrationCategory?> Delete(long id, CancellationToken cancellationToken = default)
        {
            MutableMessages.Clear();

            IntegrationCategory? category = await DbContext.Set<IntegrationCategory>()
                .AsNoTracking()
                .FirstOrDefaultAsync(current => current.Id == id, cancellationToken);

            if (category is null)
            {
                MutableMessages.Add(new KeyNotFoundException(Localizer["integration.category.notFound"]));
                return null;
            }

            if (category.IsSystem)
            {
                MutableMessages.Add(new InvalidOperationException(Localizer["integrationCategory.system.cannotDelete"]));
                return null;
            }

            return await Delete([category], cancellationToken) ? category : null;
        }
    }
}
