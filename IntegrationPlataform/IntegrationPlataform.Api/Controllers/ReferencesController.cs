using Archon.Api.Attributes;
using Archon.Core.Pagination;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Api.Controllers
{
    public sealed class ReferencesController : IntegrationPlataformReadOnlyController<Reference>
    {
        private readonly IReferenceService referenceService;

        public ReferencesController(DbContext dbContext, IReferenceService referenceService) : base(dbContext)
        {
            this.referenceService = referenceService;
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
                return Http400(Localizer["request.connector.id.required"]);
            }

            List<Reference> references = await DbContext.Set<Reference>()
                .AsNoTracking()
                .Where(item => item.ConnectorId == connectorId)
                .OrderBy(item => item.Id)
                .ToListAsync(cancellationToken);

            return Http200(references);
        }

        [RequireAccess]
        [DeleteEndpoint("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            Reference? reference = await referenceService.Delete(id, cancellationToken);
            if (reference is null)
            {
                return Http404(referenceService.GetErrorMessages());
            }

            return Http200(reference, Localizer["reference.deleted"]);
        }
    }
}
