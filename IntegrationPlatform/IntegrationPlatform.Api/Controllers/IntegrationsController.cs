using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using IntegrationPlatform.Api.Contracts.Integrations;
using IntegrationPlatform.Application.Localization;
using IntegrationPlatform.Application.Requests.Integrations;
using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace IntegrationPlatform.Api.Controllers
{
    public sealed class IntegrationsController : ApiControllerBase
    {
        private readonly IIntegrationService integrationService;
        private new IStringLocalizer<IntegrationPlatformResource> Localizer { get; }
        private static readonly Func<Integration, IntegrationContract> MapIntegration = IntegrationContract.Projection.Compile();

        public IntegrationsController(IIntegrationService integrationService, IStringLocalizer<IntegrationPlatformResource> localizer)
        {
            this.integrationService = integrationService;
            Localizer = localizer;
        }

        [RequireAccess("Permite listar as integrações cadastradas na plataforma.")]
        [GetEndpoint("[action]")]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            PagedResult<Integration> result = await integrationService.GetIntegrations(request, cancellationToken);
            return Http200(new PagedResult<IntegrationContract>
            {
                Items = result.Items.Select(MapIntegration).ToArray(),
                Pagination = result.Pagination
            });
        }

        [RequireAccess("Permite consultar os detalhes de uma integração específica.")]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            Integration? integration = await integrationService.GetIntegrationById(id, cancellationToken);

            return integration is null ? Http404(Localizer["integration.notFound"]) : Http200(MapIntegration(integration));
        }

        [RequireAccess]
        [GetEndpoint("active")]
        public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
        {
            List<Integration> integrations = await integrationService.GetActiveIntegrations(cancellationToken);
            return Http200(integrations.Select(MapIntegration).ToList());
        }

        [RequireAccess]
        [GetEndpoint("category/{categoryId:long}")]
        public async Task<IActionResult> GetByCategory(long categoryId, CancellationToken cancellationToken)
        {
            List<Integration> integrations = await integrationService.GetIntegrationsByCategory(categoryId, cancellationToken);
            return Http200(integrations.Select(MapIntegration).ToList());
        }

        [RequireAccess("Permite cadastrar uma nova integração na plataforma.")]

        [PostEndpoint("[action]")]
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

        [RequireAccess("Permite atualizar os dados de uma integração cadastrada.")]
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

        [RequireAccess("Permite excluir uma integração cadastrada na plataforma.")]
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
