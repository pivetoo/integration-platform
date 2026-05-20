using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using IntegrationPlatform.Application.Localization;
using IntegrationPlatform.Application.Requests.Connectors;
using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace IntegrationPlatform.Infrastructure.Services
{
    public sealed class ConnectorService : CrudService<Connector>, IConnectorService
    {
        private readonly IStringLocalizer<IntegrationPlatformResource> Localizer;

        public ConnectorService(DbContext dbContext, IStringLocalizer<IntegrationPlatformResource> localizer) : base(dbContext)
        {
            Localizer = localizer;
        }

        public async Task<PagedResult<Connector>> GetConnectors(PagedRequest request, string? search, CancellationToken cancellationToken = default)
        {
            var query = QueryWithDetails();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var lower = search.ToLower();
                query = query.Where(item => item.Name.ToLower().Contains(lower));
            }
            return await query
                .OrderBy(item => item.Name)
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<Connector?> GetConnectorById(long id, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<List<Connector>> GetConnectorsByIntegration(long integrationId, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .Where(item => item.IntegrationId == integrationId)
                .OrderBy(item => item.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Connector>> GetActiveConnectors(CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .Where(item => item.IsActive)
                .OrderBy(item => item.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Connector>> GetConnectorsByCategoryIdentifier(string identifier, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return new List<Connector>();
            }

            string normalized = identifier.Trim().ToLower();
            return await QueryWithDetails()
                .Where(item => item.IsActive
                    && item.Integration != null
                    && item.Integration.IsActive
                    && item.Integration.IntegrationCategory != null
                    && item.Integration.IntegrationCategory.Identifier == normalized)
                .OrderBy(item => item.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<Connector> CreateConnector(CreateConnectorRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureIntegrationExists(request.IntegrationId, cancellationToken);

            Connector connector = new(request.IntegrationId, request.Name, request.SystemApplicationId);
            bool success = await Insert(cancellationToken, connector);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            await PopulateHiddenAttributeValues(connector.Id, request.IntegrationId, cancellationToken);

            return await GetConnectorById(connector.Id, cancellationToken) ?? connector;
        }

        private async Task PopulateHiddenAttributeValues(long connectorId, long integrationId, CancellationToken cancellationToken)
        {
            List<IntegrationAttribute> hiddenAttributes = await DbContext.Set<IntegrationAttribute>()
                .AsNoTracking()
                .Where(item => item.IntegrationId == integrationId && item.IsHidden)
                .ToListAsync(cancellationToken);

            if (hiddenAttributes.Count == 0)
            {
                return;
            }

            bool added = false;
            foreach (IntegrationAttribute attribute in hiddenAttributes)
            {
                if (string.IsNullOrWhiteSpace(attribute.DefaultValue))
                {
                    // sem defaultvalue cadastrado, admin do IntegrationPlatform precisa preencher depois
                    continue;
                }

                ConnectorAttributeValue value = new(connectorId, attribute.Id, attribute.DefaultValue);
                DbContext.Set<ConnectorAttributeValue>().Add(value);
                added = true;
            }

            if (added)
            {
                await DbContext.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<Connector> UpdateConnector(long id, UpdateConnectorRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException("request.route.idMismatch");
            }

            Connector? connector = await DbContext.Set<Connector>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (connector is null)
            {
                throw new InvalidOperationException("connector.notFound");
            }

            await EnsureIntegrationExists(request.IntegrationId, cancellationToken);

            connector.Update(request.IntegrationId, request.Name, request.SystemApplicationId, request.IsActive);
            connector.EnsureWebhookToken();
            connector.SetCallback(request.CallbackUrl, request.CallbackToken);

            Connector? result = await Update(connector, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetConnectorById(result.Id, cancellationToken) ?? result;
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

        public override async Task<Connector?> Delete(long id, CancellationToken cancellationToken = default)
        {
            MutableMessages.Clear();

            Connector? connector = await QueryWithDetails()
                .FirstOrDefaultAsync(current => current.Id == id, cancellationToken);

            if (connector is null)
            {
                MutableMessages.Add(new KeyNotFoundException(Localizer["connector.notFound"]));
                return null;
            }

            List<ConnectorAttributeValue> attributeValues = await DbContext.Set<ConnectorAttributeValue>()
                .AsTracking()
                .Where(item => item.ConnectorId == id)
                .ToListAsync(cancellationToken);

            if (attributeValues.Count > 0)
            {
                DbContext.Set<ConnectorAttributeValue>().RemoveRange(attributeValues);
                await DbContext.SaveChangesAsync(cancellationToken);
            }

            return await Delete([connector], cancellationToken) ? connector : null;
        }

        private IQueryable<Connector> QueryWithDetails()
        {
            return DbContext.Set<Connector>()
                .AsNoTracking()
                .Include(item => item.Integration)
                .ThenInclude(item => item!.IntegrationCategory);
        }
    }
}
