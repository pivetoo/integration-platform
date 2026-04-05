using Archon.Api.Attributes;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using IntegrationPlataform.Api.Contracts.References;
using IntegrationPlataform.Application.Requests.References;
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

        private IQueryable<ReferenceContract> QueryContracts()
        {
            return DbContext.Set<Reference>()
                .AsNoTracking()
                .Select(ReferenceContract.Projection);
        }

        [RequireAccess]
        [GetEndpoint]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            var result = await QueryContracts()
                .OrderByDescending(item => item.CreatedAt)
                .ToPagedResultAsync(request, cancellationToken);

            return Http200(result);
        }

        [RequireAccess]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            ReferenceContract? reference = await QueryContracts()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            return reference is null ? Http404(Localizer["reference.notFound"]) : Http200(reference);
        }

        [RequireAccess]
        [GetEndpoint("connector/{connectorId:long}")]
        public async Task<IActionResult> GetByConnector(long connectorId, CancellationToken cancellationToken)
        {
            if (connectorId <= 0)
            {
                return Http400(Localizer["request.connector.id.required"]);
            }

            List<ReferenceContract> references = await QueryContracts()
                .Where(item => item.ConnectorId == connectorId)
                .OrderBy(item => item.Id)
                .ToListAsync(cancellationToken);

            return Http200(references);
        }

        [RequireAccess]
        [PostEndpoint]
        public async Task<IActionResult> Create([FromBody] CreateReferenceRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            Reference reference = new(request.ConnectorId, request.Entity, request.InternalId, request.ExternalId);
            bool success = await referenceService.Insert(cancellationToken, reference);
            if (!success)
            {
                return Http400(referenceService.GetErrorMessages());
            }

            ReferenceContract? contract = await QueryContracts()
                .FirstOrDefaultAsync(item => item.Id == reference.Id, cancellationToken);

            return Http201(contract ?? ReferenceContract.Projection.Compile()(reference), Localizer["reference.created"]);
        }

        [RequireAccess]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateReferenceRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            Reference? reference = await DbContext.Set<Reference>()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (reference is null)
            {
                return Http404(Localizer["reference.notFound"]);
            }

            reference.Update(request.Entity, request.InternalId, request.ExternalId);

            Reference? updatedReference = await referenceService.Update(reference, cancellationToken);
            if (updatedReference is null)
            {
                return Http400(referenceService.GetErrorMessages());
            }

            ReferenceContract? contract = await QueryContracts()
                .FirstOrDefaultAsync(item => item.Id == updatedReference.Id, cancellationToken);

            return Http200(contract ?? ReferenceContract.Projection.Compile()(updatedReference), Localizer["reference.updated"]);
        }

        [RequireAccess]
        [DeleteEndpoint("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            ReferenceContract? contract = await QueryContracts()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            Reference? reference = await referenceService.Delete(id, cancellationToken);
            if (reference is null)
            {
                return Http404(referenceService.GetErrorMessages());
            }

            return Http200(contract ?? ReferenceContract.Projection.Compile()(reference), Localizer["reference.deleted"]);
        }
    }
}
