using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using IntegrationPlatform.Application.Localization;
using IntegrationPlatform.Application.Requests.Integrations;
using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace IntegrationPlatform.Infrastructure.Services
{
    public sealed class IntegrationService : CrudService<Integration>, IIntegrationService
    {
        private readonly IStringLocalizer<IntegrationPlatformResource> Localizer;

        public IntegrationService(DbContext dbContext, IStringLocalizer<IntegrationPlatformResource> localizer) : base(dbContext)
        {
            Localizer = localizer;
        }

        public async Task<PagedResult<Integration>> GetIntegrations(PagedRequest request, string? search, CancellationToken cancellationToken = default)
        {
            var query = QueryWithDetails();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var lower = search.ToLower();
                query = query.Where(item => item.Name.ToLower().Contains(lower) || (item.Identifier != null && item.Identifier.ToLower().Contains(lower)));
            }
            return await query
                .OrderBy(item => item.Name)
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<Integration?> GetIntegrationById(long id, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<List<Integration>> GetActiveIntegrations(CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .Where(item => item.IsActive)
                .OrderBy(item => item.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Integration>> GetIntegrationsByCategory(long categoryId, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .Where(item => item.IntegrationCategoryId == categoryId && item.IsActive)
                .OrderBy(item => item.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<Integration> CreateIntegration(CreateIntegrationRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureUniqueIdentifier(request.Identifier, null, cancellationToken);

            Integration integration = new(request.Identifier, request.Name, request.Description, request.IntegrationCategoryId, request.IconUrl);
            bool success = await Insert(cancellationToken, integration);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            await SyncServiceContractBindings(integration.Id, integration.IntegrationCategoryId, cancellationToken);

            return await GetIntegrationById(integration.Id, cancellationToken) ?? integration;
        }

        public async Task<Integration> UpdateIntegration(long id, UpdateIntegrationRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException("request.route.idMismatch");
            }

            Integration? integration = await DbContext.Set<Integration>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (integration is null)
            {
                throw new InvalidOperationException("integration.notFound");
            }

            await EnsureUniqueIdentifier(request.Identifier, id, cancellationToken);

            integration.Update(request.Identifier, request.Name, request.Description, request.IntegrationCategoryId, request.IsActive, request.IconUrl, request.SupportsWebhook);

            Integration? result = await Update(integration, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            await SyncServiceContractBindings(result.Id, result.IntegrationCategoryId, cancellationToken);

            return await GetIntegrationById(result.Id, cancellationToken) ?? result;
        }

        private async Task SyncServiceContractBindings(long integrationId, long? integrationCategoryId, CancellationToken cancellationToken)
        {
            if (!integrationCategoryId.HasValue)
            {
                return;
            }

            List<long> serviceContractIds = await DbContext.Set<ServiceContract>()
                .AsNoTracking()
                .Where(item => item.IntegrationCategoryId == integrationCategoryId.Value && item.IsActive)
                .Select(item => item.Id)
                .ToListAsync(cancellationToken);

            if (serviceContractIds.Count == 0)
            {
                return;
            }

            HashSet<long> alreadyBound = (await DbContext.Set<IntegrationServiceContract>()
                .AsNoTracking()
                .Where(item => item.IntegrationId == integrationId)
                .Select(item => item.ServiceContractId)
                .ToListAsync(cancellationToken))
                .ToHashSet();

            foreach (long serviceContractId in serviceContractIds)
            {
                if (alreadyBound.Contains(serviceContractId))
                {
                    continue;
                }

                IntegrationServiceContract binding = new(integrationId, serviceContractId);
                await DbContext.Set<IntegrationServiceContract>().AddAsync(binding, cancellationToken);
            }

            await DbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task EnsureUniqueIdentifier(string identifier, long? currentId, CancellationToken cancellationToken)
        {
            bool identifierExists = await DbContext.Set<Integration>()
                .AsNoTracking()
                .AnyAsync(
                    item => item.Identifier == identifier &&
                    (!currentId.HasValue || item.Id != currentId.Value),
                    cancellationToken);

            if (identifierExists)
            {
                throw new InvalidOperationException("integration.identifier.alreadyExists");
            }
        }

        public override async Task<Integration?> Delete(long id, CancellationToken cancellationToken = default)
        {
            MutableMessages.Clear();

            Integration? integration = await QueryWithDetails()
                .FirstOrDefaultAsync(current => current.Id == id, cancellationToken);

            if (integration is null)
            {
                MutableMessages.Add(new KeyNotFoundException(Localizer["integration.notFound"]));
                return null;
            }

            return await Delete([integration], cancellationToken) ? integration : null;
        }

        private IQueryable<Integration> QueryWithDetails()
        {
            return DbContext.Set<Integration>()
                .AsNoTracking()
                .Include(item => item.IntegrationCategory);
        }
    }
}
