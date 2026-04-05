using Archon.Api.Attributes;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using IntegrationPlataform.Api.Contracts.IntegrationAttributes;
using IntegrationPlataform.Application.Requests.IntegrationAttributes;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Api.Controllers
{
    public sealed class IntegrationAttributesController : IntegrationPlataformReadOnlyController<IntegrationAttribute>
    {
        private readonly IIntegrationAttributeService integrationAttributeService;

        public IntegrationAttributesController(DbContext dbContext, IIntegrationAttributeService integrationAttributeService) : base(dbContext)
        {
            this.integrationAttributeService = integrationAttributeService;
        }

        private IQueryable<IntegrationAttributeContract> QueryContracts()
        {
            return DbContext.Set<IntegrationAttribute>()
                .AsNoTracking()
                .Select(IntegrationAttributeContract.Projection);
        }

        [RequireAccess]
        [GetEndpoint]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            var result = await QueryContracts()
                .OrderBy(item => item.Order)
                .ToPagedResultAsync(request, cancellationToken);

            return Http200(result);
        }

        [RequireAccess]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            IntegrationAttributeContract? attribute = await QueryContracts()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            return attribute is null ? Http404(Localizer["integration.attribute.notFound"]) : Http200(attribute);
        }

        [RequireAccess]
        [GetEndpoint("integration/{integrationId:long}")]
        public async Task<IActionResult> GetByIntegration(long integrationId, CancellationToken cancellationToken)
        {
            if (integrationId <= 0)
            {
                return Http400(Localizer["request.integration.id.required"]);
            }

            List<IntegrationAttributeContract> attributes = await QueryContracts()
                .Where(item => item.IntegrationId == integrationId)
                .OrderBy(item => item.Order)
                .ToListAsync(cancellationToken);

            return Http200(attributes);
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

            IntegrationAttributeContract? contract = await QueryContracts()
                .FirstOrDefaultAsync(item => item.Id == attribute.Id, cancellationToken);

            return Http201(contract ?? IntegrationAttributeContract.Projection.Compile()(attribute), Localizer["integration.attribute.created"]);
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

            if (id != request.Id)
            {
                return Http400(Localizer["request.route.idMismatch"]);
            }

            IntegrationAttribute? attribute = await DbContext.Set<IntegrationAttribute>()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (attribute is null)
            {
                return Http404(Localizer["integration.attribute.notFound"]);
            }

            attribute.Update(
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

            IntegrationAttribute? updatedAttribute = await integrationAttributeService.Update(attribute, cancellationToken);
            if (updatedAttribute is null)
            {
                return Http400(integrationAttributeService.GetErrorMessages());
            }

            IntegrationAttributeContract? contract = await QueryContracts()
                .FirstOrDefaultAsync(item => item.Id == updatedAttribute.Id, cancellationToken);

            return Http200(contract ?? IntegrationAttributeContract.Projection.Compile()(updatedAttribute), Localizer["integration.attribute.updated"]);
        }

        [RequireAccess]
        [DeleteEndpoint("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            IntegrationAttributeContract? contract = await QueryContracts()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            IntegrationAttribute? attribute = await integrationAttributeService.Delete(id, cancellationToken);
            if (attribute is null)
            {
                return Http404(integrationAttributeService.GetErrorMessages());
            }

            return Http200(contract ?? IntegrationAttributeContract.Projection.Compile()(attribute), Localizer["integration.attribute.deleted"]);
        }
    }
}
