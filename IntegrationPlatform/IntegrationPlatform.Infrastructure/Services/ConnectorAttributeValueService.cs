using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using IntegrationPlatform.Application.Localization;
using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace IntegrationPlatform.Infrastructure.Services
{
    public sealed class ConnectorAttributeValueService : CrudService<ConnectorAttributeValue>, IConnectorAttributeValueService
    {
        private readonly IStringLocalizer<IntegrationPlatformResource> Localizer;

        public ConnectorAttributeValueService(DbContext dbContext, IStringLocalizer<IntegrationPlatformResource> localizer) : base(dbContext)
        {
            Localizer = localizer;
        }

        public async Task<PagedResult<ConnectorAttributeValue>> GetConnectorAttributeValues(PagedRequest request, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<ConnectorAttributeValue>()
                .AsNoTracking()
                .OrderByDescending(item => item.Id)
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<ConnectorAttributeValue?> GetConnectorAttributeValueById(long id, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<ConnectorAttributeValue>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<List<ConnectorAttributeValue>> GetConnectorAttributeValuesByConnector(long connectorId, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<ConnectorAttributeValue>()
                .AsNoTracking()
                .Where(item => item.ConnectorId == connectorId)
                .OrderBy(item => item.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<ConnectorAttributeValue> UpdateConnectorAttributeValue(long id, long requestId, long integrationAttributeId, string value, CancellationToken cancellationToken = default)
        {
            if (id != requestId)
            {
                throw new InvalidOperationException("request.route.idMismatch");
            }

            ConnectorAttributeValue? attributeValue = await DbContext.Set<ConnectorAttributeValue>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (attributeValue is null)
            {
                throw new InvalidOperationException("connector.attributeValue.notFound");
            }

            attributeValue.Update(integrationAttributeId, value);

            ConnectorAttributeValue? result = await Update(attributeValue, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return result;
        }

        public override async Task<ConnectorAttributeValue?> Delete(long id, CancellationToken cancellationToken = default)
        {
            MutableMessages.Clear();

            ConnectorAttributeValue? value = await DbContext.Set<ConnectorAttributeValue>()
                .AsNoTracking()
                .FirstOrDefaultAsync(current => current.Id == id, cancellationToken);

            if (value is null)
            {
                MutableMessages.Add(new KeyNotFoundException(Localizer["connector.attributeValue.notFound"]));
                return null;
            }

            return await Delete([value], cancellationToken) ? value : null;
        }
    }
}
