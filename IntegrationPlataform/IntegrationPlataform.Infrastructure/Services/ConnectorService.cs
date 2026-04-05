using Archon.Infrastructure.Services;
using IntegrationPlataform.Application.Localization;
using IntegrationPlataform.Application.Requests.Connectors;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace IntegrationPlataform.Infrastructure.Services
{
    public sealed class ConnectorService : CrudService<Connector>, IConnectorService
    {
        private readonly IStringLocalizer<IntegrationPlataformResource> Localizer;

        public ConnectorService(DbContext dbContext, IStringLocalizer<IntegrationPlataformResource> localizer) : base(dbContext)
        {
            Localizer = localizer;
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

            return connector;
        }

        public async Task<Connector> UpdateConnector(long id, UpdateConnectorRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException(Localizer["request.route.idMismatch"]);
            }

            Connector? connector = await DbContext.Set<Connector>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (connector is null)
            {
                throw new InvalidOperationException(Localizer["connector.notFound"]);
            }

            await EnsureIntegrationExists(request.IntegrationId, cancellationToken);

            connector.Update(request.IntegrationId, request.Name, request.SystemApplicationId, request.IsActive);

            Connector? result = await Update(connector, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return result;
        }

        private async Task EnsureIntegrationExists(long integrationId, CancellationToken cancellationToken)
        {
            bool exists = await DbContext.Set<Integration>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == integrationId, cancellationToken);

            if (!exists)
            {
                throw new InvalidOperationException(Localizer["integration.notFound"]);
            }
        }

        public override async Task<Connector?> Delete(long id, CancellationToken cancellationToken = default)
        {
            MutableMessages.Clear();

            Connector? connector = await DbContext.Set<Connector>()
                .AsNoTracking()
                .FirstOrDefaultAsync(current => current.Id == id, cancellationToken);

            if (connector is null)
            {
                MutableMessages.Add(new KeyNotFoundException(Localizer["connector.notFound"]));
                return null;
            }

            return await Delete([connector], cancellationToken) ? connector : null;
        }
    }
}
