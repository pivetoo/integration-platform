using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using IntegrationPlataform.Api.Contracts.Connectors;
using IntegrationPlataform.Application.Requests.Connectors;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Api.Controllers
{
    public sealed class ConnectorsController : ReadOnlyController<Connector>
    {
        private readonly IConnectorService connectorService;

        public ConnectorsController(DbContext dbContext, IConnectorService connectorService) : base(dbContext)
        {
            this.connectorService = connectorService;
        }

        [RequireAccess]
        [GetEndpoint]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            var result = await DbContext.Set<Connector>()
                .AsNoTracking()
                .OrderBy(item => item.Name)
                .Select(ConnectorContract.Projection)
                .ToPagedResultAsync(request, cancellationToken);

            return Http200(result);
        }

        [RequireAccess]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            var connector = await DbContext.Set<Connector>()
                .AsNoTracking()
                .Where(item => item.Id == id)
                .Select(ConnectorContract.Projection)
                .FirstOrDefaultAsync(cancellationToken);

            return connector is null ? Http404("Record not found.") : Http200(connector);
        }

        [RequireAccess]
        [GetEndpoint("integration/{integrationId:long}")]
        public async Task<IActionResult> GetByIntegration(long integrationId, CancellationToken cancellationToken)
        {
            if (integrationId <= 0)
            {
                return Http400("Integration id is required.");
            }

            var connectors = await DbContext.Set<Connector>()
                .AsNoTracking()
                .Where(item => item.IntegrationId == integrationId)
                .OrderBy(item => item.Name)
                .Select(ConnectorContract.Projection)
                .ToListAsync(cancellationToken);

            return Http200(connectors);
        }

        [RequireAccess]
        [GetEndpoint("active")]
        public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
        {
            var connectors = await DbContext.Set<Connector>()
                .AsNoTracking()
                .Where(item => item.IsActive)
                .OrderBy(item => item.Name)
                .Select(ConnectorContract.Projection)
                .ToListAsync(cancellationToken);

            return Http200(connectors);
        }

        [RequireAccess]
        [PostEndpoint]
        public async Task<IActionResult> Create([FromBody] CreateConnectorRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            Connector connector = await connectorService.CreateConnector(request, cancellationToken);
            return Http201(connector, "Connector created successfully.");
        }

        [RequireAccess]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateConnectorRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            Connector connector = await connectorService.UpdateConnector(id, request, cancellationToken);
            return Http200(connector, "Connector updated successfully.");
        }

        [RequireAccess]
        [DeleteEndpoint("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            Connector? connector = await connectorService.Delete(id, cancellationToken);
            if (connector is null)
            {
                return Http404(connectorService.GetErrorMessages());
            }

            return Http200(connector, "Connector deleted successfully.");
        }
    }
}
