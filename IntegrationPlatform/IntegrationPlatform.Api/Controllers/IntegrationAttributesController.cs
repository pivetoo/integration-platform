using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using IntegrationPlatform.Api.Contracts.IntegrationAttributes;
using IntegrationPlatform.Application.Localization;
using IntegrationPlatform.Application.Requests.IntegrationAttributes;
using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace IntegrationPlatform.Api.Controllers
{
    public sealed class IntegrationAttributesController : ApiControllerBase
    {
        private readonly IIntegrationAttributeService integrationAttributeService;
        private new IStringLocalizer<IntegrationPlatformResource> Localizer { get; }
        private static readonly Func<IntegrationAttribute, IntegrationAttributeContract> MapIntegrationAttribute = IntegrationAttributeContract.Projection.Compile();

        public IntegrationAttributesController(IIntegrationAttributeService integrationAttributeService, IStringLocalizer<IntegrationPlatformResource> localizer)
        {
            this.integrationAttributeService = integrationAttributeService;
            Localizer = localizer;
        }

        [RequireAccess("Permite listar os atributos configuráveis das integrações.")]
        [GetEndpoint("[action]")]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            PagedResult<IntegrationAttribute> result = await integrationAttributeService.GetIntegrationAttributes(request, cancellationToken);
            return Http200(new PagedResult<IntegrationAttributeContract>
            {
                Items = result.Items.Select(MapIntegrationAttribute).ToArray(),
                Pagination = result.Pagination
            });
        }

        [RequireAccess("Permite consultar os detalhes de um atributo de integração específico.")]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            IntegrationAttribute? attribute = await integrationAttributeService.GetIntegrationAttributeById(id, cancellationToken);

            return attribute is null ? Http404(Localizer["integration.attribute.notFound"]) : Http200(MapIntegrationAttribute(attribute));
        }

        [RequireAccess]
        [GetEndpoint("integration/{integrationId:long}")]
        public async Task<IActionResult> GetByIntegration(long integrationId, CancellationToken cancellationToken)
        {
            if (integrationId <= 0)
            {
                return Http400(Localizer["request.integration.id.required"]);
            }

            List<IntegrationAttribute> attributes = await integrationAttributeService.GetIntegrationAttributesByIntegration(integrationId, cancellationToken);

            return Http200(attributes.Select(MapIntegrationAttribute).ToList());
        }

        [RequireAccess("Permite cadastrar um novo atributo configurável para uma integração.")]
        [PostEndpoint("[action]")]
        public async Task<IActionResult> Create([FromBody] CreateIntegrationAttributeRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            IntegrationAttribute attribute = new(
                request.IntegrationId,
                request.Field,
                request.Label,
                request.Type,
                request.IsRequired,
                request.Order,
                request.Description,
                request.Placeholder,
                request.DefaultValue,
                request.Group,
                request.IsSensitive);

            bool success = await integrationAttributeService.Insert(cancellationToken, attribute);
            if (!success)
            {
                return Http400(integrationAttributeService.GetErrorMessages());
            }

            return Http201(MapIntegrationAttribute(attribute), Localizer["integration.attribute.created"]);
        }

        [RequireAccess("Permite atualizar um atributo configurável de integração.")]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateIntegrationAttributeRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            IntegrationAttribute updatedAttribute = await integrationAttributeService.UpdateIntegrationAttribute(
                id,
                request.Id,
                request.Field,
                request.Label,
                request.Type,
                request.IsRequired,
                request.Order,
                request.Description,
                request.Placeholder,
                request.DefaultValue,
                request.Group,
                request.IsSensitive,
                cancellationToken);

            return Http200(MapIntegrationAttribute(updatedAttribute), Localizer["integration.attribute.updated"]);
        }

        [RequireAccess("Permite excluir um atributo configurável de integração.")]
        [DeleteEndpoint("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            IntegrationAttribute? attribute = await integrationAttributeService.Delete(id, cancellationToken);
            if (attribute is null)
            {
                return Http404(integrationAttributeService.GetErrorMessages());
            }

            return Http200(MapIntegrationAttribute(attribute), Localizer["integration.attribute.deleted"]);
        }
    }
}
