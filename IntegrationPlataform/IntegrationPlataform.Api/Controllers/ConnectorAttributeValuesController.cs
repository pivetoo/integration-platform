using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using IntegrationPlataform.Application.Localization;
using IntegrationPlataform.Application.Requests.ConnectorAttributeValues;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace IntegrationPlataform.Api.Controllers
{
    public sealed class ConnectorAttributeValuesController : ApiControllerBase
    {
        private readonly IConnectorAttributeValueService connectorAttributeValueService;
        private new IStringLocalizer<IntegrationPlataformResource> Localizer { get; }

        public ConnectorAttributeValuesController(IConnectorAttributeValueService connectorAttributeValueService, IStringLocalizer<IntegrationPlataformResource> localizer)
        {
            this.connectorAttributeValueService = connectorAttributeValueService;
            Localizer = localizer;
        }

        [RequireAccess]
        [GetEndpoint]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            PagedResult<ConnectorAttributeValue> result = await connectorAttributeValueService.GetConnectorAttributeValues(request, cancellationToken);
            return Http200(result);
        }

        [RequireAccess]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return Http400(Localizer["request.id.required"]);
            }

            ConnectorAttributeValue? entity = await connectorAttributeValueService.GetConnectorAttributeValueById(id, cancellationToken);
            return entity is null ? Http404(Localizer["record.notFound"]) : Http200(entity);
        }

        [RequireAccess]
        [GetEndpoint("connector/{connectorId:long}")]
        public async Task<IActionResult> GetByConnector(long connectorId, CancellationToken cancellationToken)
        {
            if (connectorId <= 0)
            {
                return Http400(Localizer["request.connector.id.required"]);
            }

            List<ConnectorAttributeValue> values = await connectorAttributeValueService.GetConnectorAttributeValuesByConnector(connectorId, cancellationToken);
            return Http200(values);
        }

        [RequireAccess]
        [PostEndpoint]
        public async Task<IActionResult> Create([FromBody] CreateConnectorAttributeValueRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            ConnectorAttributeValue value = new(request.ConnectorId, request.IntegrationAttributeId, request.Value);
            bool success = await connectorAttributeValueService.Insert(cancellationToken, value);
            if (!success)
            {
                return Http400(connectorAttributeValueService.GetErrorMessages());
            }

            return Http201(value, Localizer["connector.attributeValue.created"]);
        }

        [RequireAccess]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateConnectorAttributeValueRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            ConnectorAttributeValue updatedValue = await connectorAttributeValueService.UpdateConnectorAttributeValue(
                id,
                request.Id,
                request.IntegrationAttributeId,
                request.Value,
                cancellationToken);
            return Http200(updatedValue, Localizer["connector.attributeValue.updated"]);
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

            return Http200(value, Localizer["connector.attributeValue.deleted"]);
        }
    }
}
