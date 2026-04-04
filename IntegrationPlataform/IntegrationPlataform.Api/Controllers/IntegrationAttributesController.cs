using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Api.Controllers
{
    public sealed class IntegrationAttributesController : ReadOnlyController<IntegrationAttribute>
    {
        private readonly IIntegrationAttributeService integrationAttributeService;

        public IntegrationAttributesController(DbContext dbContext, IIntegrationAttributeService integrationAttributeService) : base(dbContext)
        {
            this.integrationAttributeService = integrationAttributeService;
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
        [GetEndpoint("integration/{integrationId:long}")]
        public async Task<IActionResult> GetByIntegration(long integrationId, CancellationToken cancellationToken)
        {
            if (integrationId <= 0)
            {
                return Http400("Integration id is required.");
            }

            List<IntegrationAttribute> attributes = await DbContext.Set<IntegrationAttribute>()
                .AsNoTracking()
                .Where(item => item.IntegrationId == integrationId)
                .OrderBy(item => item.Order)
                .ToListAsync(cancellationToken);

            return Http200(attributes);
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

            return Http200(attribute, "Integration attribute deleted successfully.");
        }
    }
}
