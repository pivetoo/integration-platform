using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using IntegrationPlataform.Api.Contracts.IntegrationAttributes;
using IntegrationPlataform.Application.Localization;
using IntegrationPlataform.Application.Requests.IntegrationAttributes;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace IntegrationPlataform.Api.Controllers
{
    public sealed class IntegrationAttributesController : ApiControllerBase
    {
        private readonly IIntegrationAttributeService integrationAttributeService;
        private new IStringLocalizer<IntegrationPlataformResource> Localizer { get; }
        private static readonly Func<IntegrationAttribute, IntegrationAttributeContract> MapIntegrationAttribute = IntegrationAttributeContract.Projection.Compile();

        public IntegrationAttributesController(IIntegrationAttributeService integrationAttributeService, IStringLocalizer<IntegrationPlataformResource> localizer)
        {
            this.integrationAttributeService = integrationAttributeService;
            Localizer = localizer;
        }

        [RequireAccess]
        [GetEndpoint]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            PagedResult<IntegrationAttribute> result = await integrationAttributeService.GetIntegrationAttributes(request, cancellationToken);
            return Http200(new PagedResult<IntegrationAttributeContract>
            {
                Items = result.Items.Select(MapIntegrationAttribute).ToArray(),
                Pagination = result.Pagination
            });
        }

        [RequireAccess]
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

        [RequireAccess]
        [PostEndpoint]
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

        [RequireAccess]
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

        [RequireAccess]
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
