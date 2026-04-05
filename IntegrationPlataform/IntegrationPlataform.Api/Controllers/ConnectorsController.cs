using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using IntegrationPlataform.Api.Contracts.Connectors;
using IntegrationPlataform.Application.Localization;
using IntegrationPlataform.Application.Requests.Connectors;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace IntegrationPlataform.Api.Controllers
{
    public sealed class ConnectorsController : ApiControllerBase
    {
        private readonly IConnectorService connectorService;
        private new IStringLocalizer<IntegrationPlataformResource> Localizer { get; }
        private static readonly Func<Connector, ConnectorContract> MapConnector = ConnectorContract.Projection.Compile();

        public ConnectorsController(IConnectorService connectorService, IStringLocalizer<IntegrationPlataformResource> localizer)
        {
            this.connectorService = connectorService;
            Localizer = localizer;
        }

        [RequireAccess("Permite listar os conectores cadastrados na plataforma.")]
        [GetEndpoint]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            PagedResult<Connector> result = await connectorService.GetConnectors(request, cancellationToken);
            return Http200(new PagedResult<ConnectorContract>
            {
                Items = result.Items.Select(MapConnector).ToArray(),
                Pagination = result.Pagination
            });
        }

        [RequireAccess("Permite consultar os detalhes de um conector específico.")]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            Connector? connector = await connectorService.GetConnectorById(id, cancellationToken);

            return connector is null ? Http404(Localizer["connector.notFound"]) : Http200(MapConnector(connector));
        }

        [RequireAccess("Permite listar os conectores vinculados a uma integração específica.")]
        [GetEndpoint("integration/{integrationId:long}")]
        public async Task<IActionResult> GetByIntegration(long integrationId, CancellationToken cancellationToken)
        {
            if (integrationId <= 0)
            {
                return Http400(Localizer["request.integration.id.required"]);
            }

            List<Connector> connectors = await connectorService.GetConnectorsByIntegration(integrationId, cancellationToken);

            return Http200(connectors.Select(MapConnector).ToList());
        }

        [RequireAccess("Permite listar apenas os conectores ativos.")]
        [GetEndpoint("active")]
        public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
        {
            List<Connector> connectors = await connectorService.GetActiveConnectors(cancellationToken);

            return Http200(connectors.Select(MapConnector).ToList());
        }

        [RequireAccess("Permite cadastrar um novo conector para uma integração.")]
        [PostEndpoint]
        public async Task<IActionResult> Create([FromBody] CreateConnectorRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            Connector connector = await connectorService.CreateConnector(request, cancellationToken);
            return Http201(MapConnector(connector), Localizer["connector.created"]);
        }

        [RequireAccess("Permite atualizar a configuração de um conector cadastrado.")]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateConnectorRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            Connector connector = await connectorService.UpdateConnector(id, request, cancellationToken);
            return Http200(MapConnector(connector), Localizer["connector.updated"]);
        }

        [RequireAccess("Permite excluir um conector cadastrado na plataforma.")]
        [DeleteEndpoint("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            Connector? connector = await connectorService.Delete(id, cancellationToken);
            if (connector is null)
            {
                return Http404(connectorService.GetErrorMessages());
            }

            return Http200(MapConnector(connector), Localizer["connector.deleted"]);
        }
    }
}
