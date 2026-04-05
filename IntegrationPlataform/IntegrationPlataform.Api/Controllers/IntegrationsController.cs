using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using IntegrationPlataform.Api.Contracts.Integrations;
using IntegrationPlataform.Application.Localization;
using IntegrationPlataform.Application.Requests.Integrations;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace IntegrationPlataform.Api.Controllers
{
    public sealed class IntegrationsController : ApiControllerBase
    {
        private readonly IIntegrationService integrationService;
        private new IStringLocalizer<IntegrationPlataformResource> Localizer { get; }
        private static readonly Func<Integration, IntegrationContract> MapIntegration = IntegrationContract.Projection.Compile();

        public IntegrationsController(IIntegrationService integrationService, IStringLocalizer<IntegrationPlataformResource> localizer)
        {
            this.integrationService = integrationService;
            Localizer = localizer;
        }

        [RequireAccess]
        [GetEndpoint]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            PagedResult<Integration> result = await integrationService.GetIntegrations(request, cancellationToken);
            return Http200(new PagedResult<IntegrationContract>
            {
                Items = result.Items.Select(MapIntegration).ToArray(),
                Pagination = result.Pagination
            });
        }

        [RequireAccess]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            Integration? integration = await integrationService.GetIntegrationById(id, cancellationToken);

            return integration is null ? Http404(Localizer["integration.notFound"]) : Http200(integration);
        }

        [RequireAccess]
        [GetEndpoint("active")]
        public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
        {
            List<Integration> integrations = await integrationService.GetActiveIntegrations(cancellationToken);
            return Http200(integrations.Select(MapIntegration).ToList());
        }

        [RequireAccess]
        [PostEndpoint]
        public async Task<IActionResult> Create([FromBody] CreateIntegrationRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            Integration integration = await integrationService.CreateIntegration(request, cancellationToken);
            return Http201(MapIntegration(integration), Localizer["integration.created"]);
        }

        [RequireAccess]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateIntegrationRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            Integration integration = await integrationService.UpdateIntegration(id, request, cancellationToken);
            return Http200(MapIntegration(integration), Localizer["integration.updated"]);
        }

        [RequireAccess]
        [DeleteEndpoint("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            Integration? integration = await integrationService.Delete(id, cancellationToken);
            if (integration is null)
            {
                return Http404(integrationService.GetErrorMessages());
            }

            return Http200(MapIntegration(integration), Localizer["integration.deleted"]);
        }
    }
}
