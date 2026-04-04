using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Api.Controllers
{
    public sealed class ConnectorAttributeValuesController : ReadOnlyController<ConnectorAttributeValue>
    {
        private readonly IConnectorAttributeValueService connectorAttributeValueService;

        public ConnectorAttributeValuesController(DbContext dbContext, IConnectorAttributeValueService connectorAttributeValueService) : base(dbContext)
        {
            this.connectorAttributeValueService = connectorAttributeValueService;
        }

        [RequireAccess]
        [GetEndpoint]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            return await base.Get(request, cancellationToken);
        }

        [RequireAccess]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            return await base.GetById(id, cancellationToken);
        }

        [RequireAccess]
        [GetEndpoint("connector/{connectorId:long}")]
        public async Task<IActionResult> GetByConnector(long connectorId, CancellationToken cancellationToken)
        {
            if (connectorId <= 0)
            {
                return Http400("Connector id is required.");
            }

            List<ConnectorAttributeValue> values = await DbContext.Set<ConnectorAttributeValue>()
                .AsNoTracking()
                .Where(item => item.ConnectorId == connectorId)
                .OrderBy(item => item.Id)
                .ToListAsync(cancellationToken);

            return Http200(values);
        }

        [RequireAccess]
        [DeleteEndpoint("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            ConnectorAttributeValue? value = await connectorAttributeValueService.Delete(id, cancellationToken);
            if (value is null)
            {
                return Http404(connectorAttributeValueService.GetErrorMessages());
            }

            return Http200(value, "Connector attribute value deleted successfully.");
        }
    }
}
