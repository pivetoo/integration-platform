using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using IntegrationPlatform.Application.Localization;
using IntegrationPlatform.Application.Requests.ServiceContracts;
using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace IntegrationPlatform.Infrastructure.Services
{
    public sealed class ServiceContractService : CrudService<ServiceContract>, IServiceContractService
    {
        private readonly IStringLocalizer<IntegrationPlatformResource> Localizer;

        public ServiceContractService(DbContext dbContext, IStringLocalizer<IntegrationPlatformResource> localizer) : base(dbContext)
        {
            Localizer = localizer;
        }

        public async Task<PagedResult<ServiceContract>> GetServiceContracts(PagedRequest request, string? search, long? integrationCategoryId, CancellationToken cancellationToken = default)
        {
            var query = DbContext.Set<ServiceContract>()
                .AsNoTracking()
                .Include(item => item.IntegrationCategory)
                .AsQueryable();

            if (integrationCategoryId.HasValue && integrationCategoryId.Value > 0)
            {
                query = query.Where(item => item.IntegrationCategoryId == integrationCategoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string lower = search.ToLower();
                query = query.Where(item =>
                    item.Name.ToLower().Contains(lower)
                    || item.Identifier.ToLower().Contains(lower)
                    || (item.Description != null && item.Description.ToLower().Contains(lower)));
            }

            return await query
                .OrderBy(item => item.Identifier)
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<ServiceContract?> GetServiceContractById(long id, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<ServiceContract>()
                .AsNoTracking()
                .Include(item => item.IntegrationCategory)
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<ServiceContract?> GetServiceContractByIdentifier(string identifier, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return null;
            }

            string normalized = identifier.Trim().ToLowerInvariant();
            return await DbContext.Set<ServiceContract>()
                .AsNoTracking()
                .Include(item => item.IntegrationCategory)
                .FirstOrDefaultAsync(item => item.Identifier == normalized, cancellationToken);
        }

        public async Task<List<ServiceContract>> GetActiveServiceContracts(CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<ServiceContract>()
                .AsNoTracking()
                .Include(item => item.IntegrationCategory)
                .Where(item => item.IsActive)
                .OrderBy(item => item.Identifier)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<ServiceContract>> GetServiceContractsByIntegration(long integrationId, CancellationToken cancellationToken = default)
        {
            if (integrationId <= 0)
            {
                return [];
            }

            return await DbContext.Set<IntegrationServiceContract>()
                .AsNoTracking()
                .Where(item => item.IntegrationId == integrationId && item.IsActive)
                .Include(item => item.ServiceContract)
                .ThenInclude(item => item.IntegrationCategory)
                .Select(item => item.ServiceContract)
                .OrderBy(item => item.Identifier)
                .ToListAsync(cancellationToken);
        }

        public async Task<ServiceContract> CreateServiceContract(CreateServiceContractRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureIdentifierIsUnique(request.Identifier, ignoreId: null, cancellationToken);
            await EnsureCategoryExists(request.IntegrationCategoryId, cancellationToken);

            ServiceContract entity = new(
                request.Identifier,
                request.Name,
                request.IntegrationCategoryId,
                request.Description,
                request.InputSchema,
                request.OutputSchema,
                request.HasCallback,
                request.CallbackSchema);

            bool success = await Insert(cancellationToken, entity);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return entity;
        }

        public async Task<ServiceContract> UpdateServiceContract(long id, UpdateServiceContractRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException("request.route.idMismatch");
            }

            ServiceContract? entity = await DbContext.Set<ServiceContract>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (entity is null)
            {
                throw new InvalidOperationException("serviceContract.notFound");
            }

            await EnsureIdentifierIsUnique(request.Identifier, ignoreId: id, cancellationToken);
            await EnsureCategoryExists(request.IntegrationCategoryId, cancellationToken);

            entity.Update(
                request.Identifier,
                request.Name,
                request.IntegrationCategoryId,
                request.Description,
                request.InputSchema,
                request.OutputSchema,
                request.HasCallback,
                request.CallbackSchema,
                request.IsActive);

            ServiceContract? result = await Update(entity, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return result;
        }

        public async Task<IntegrationServiceContract> SetIntegrationServiceContract(SetIntegrationServiceContractRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureIntegrationExists(request.IntegrationId, cancellationToken);
            await EnsureServiceContractExists(request.ServiceContractId, cancellationToken);

            IntegrationServiceContract? existing = await DbContext.Set<IntegrationServiceContract>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.IntegrationId == request.IntegrationId && item.ServiceContractId == request.ServiceContractId, cancellationToken);

            if (existing is not null)
            {
                if (request.IsActive)
                {
                    existing.Activate();
                }
                else
                {
                    existing.Deactivate();
                }

                await DbContext.SaveChangesAsync(cancellationToken);
                return existing;
            }

            IntegrationServiceContract created = new(request.IntegrationId, request.ServiceContractId);
            if (!request.IsActive)
            {
                created.Deactivate();
            }

            await DbContext.Set<IntegrationServiceContract>().AddAsync(created, cancellationToken);
            await DbContext.SaveChangesAsync(cancellationToken);
            return created;
        }

        public async Task<bool> RemoveIntegrationServiceContract(long integrationId, long serviceContractId, CancellationToken cancellationToken = default)
        {
            IntegrationServiceContract? entity = await DbContext.Set<IntegrationServiceContract>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.IntegrationId == integrationId && item.ServiceContractId == serviceContractId, cancellationToken);

            if (entity is null)
            {
                return false;
            }

            DbContext.Set<IntegrationServiceContract>().Remove(entity);
            await DbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        public override async Task<ServiceContract?> Delete(long id, CancellationToken cancellationToken = default)
        {
            MutableMessages.Clear();

            ServiceContract? entity = await DbContext.Set<ServiceContract>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (entity is null)
            {
                MutableMessages.Add(new KeyNotFoundException(Localizer["serviceContract.notFound"]));
                return null;
            }

            if (entity.IsSystem)
            {
                MutableMessages.Add(new InvalidOperationException(Localizer["serviceContract.system.cannotDelete"]));
                return null;
            }

            return await Delete([entity], cancellationToken) ? entity : null;
        }

        private async Task EnsureIdentifierIsUnique(string identifier, long? ignoreId, CancellationToken cancellationToken)
        {
            string normalized = identifier.Trim().ToLowerInvariant();

            bool exists = await DbContext.Set<ServiceContract>()
                .AsNoTracking()
                .AnyAsync(item => item.Identifier == normalized && (ignoreId == null || item.Id != ignoreId), cancellationToken);

            if (exists)
            {
                throw new InvalidOperationException("serviceContract.identifier.duplicated");
            }
        }

        private async Task EnsureCategoryExists(long integrationCategoryId, CancellationToken cancellationToken)
        {
            bool exists = await DbContext.Set<IntegrationCategory>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == integrationCategoryId, cancellationToken);

            if (!exists)
            {
                throw new InvalidOperationException("integration.category.notFound");
            }
        }

        private async Task EnsureIntegrationExists(long integrationId, CancellationToken cancellationToken)
        {
            bool exists = await DbContext.Set<Integration>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == integrationId, cancellationToken);

            if (!exists)
            {
                throw new InvalidOperationException("integration.notFound");
            }
        }

        private async Task EnsureServiceContractExists(long serviceContractId, CancellationToken cancellationToken)
        {
            bool exists = await DbContext.Set<ServiceContract>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == serviceContractId, cancellationToken);

            if (!exists)
            {
                throw new InvalidOperationException("serviceContract.notFound");
            }
        }
    }
}
