using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using IntegrationPlatform.Application.Localization;
using IntegrationPlatform.Application.Requests.References;
using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace IntegrationPlatform.Infrastructure.Services
{
    public sealed class ReferenceService : CrudService<Reference>, IReferenceService
    {
        private readonly IStringLocalizer<IntegrationPlatformResource> Localizer;

        public ReferenceService(DbContext dbContext, IStringLocalizer<IntegrationPlatformResource> localizer) : base(dbContext)
        {
            Localizer = localizer;
        }

        public async Task<PagedResult<Reference>> GetReferences(PagedRequest request, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .OrderByDescending(item => item.CreatedAt)
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<Reference?> GetReferenceById(long id, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<List<Reference>> GetReferencesByConnector(long connectorId, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .Where(item => item.ConnectorId == connectorId)
                .OrderBy(item => item.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<Reference> CreateReference(CreateReferenceRequest request, CancellationToken cancellationToken = default)
        {
            Reference reference = new(request.ConnectorId, request.Entity, request.InternalId, request.ExternalId);
            bool success = await Insert(cancellationToken, reference);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetReferenceById(reference.Id, cancellationToken) ?? reference;
        }

        public async Task<Reference> UpdateReference(long id, UpdateReferenceRequest request, CancellationToken cancellationToken = default)
        {
            Reference? reference = await DbContext.Set<Reference>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (reference is null)
            {
                throw new InvalidOperationException(Localizer["reference.notFound"]);
            }

            reference.Update(request.Entity, request.InternalId, request.ExternalId);

            Reference? result = await Update(reference, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetReferenceById(result.Id, cancellationToken) ?? result;
        }

        public override async Task<Reference?> Delete(long id, CancellationToken cancellationToken = default)
        {
            MutableMessages.Clear();

            Reference? reference = await GetReferenceById(id, cancellationToken);

            if (reference is null)
            {
                MutableMessages.Add(new KeyNotFoundException(Localizer["reference.notFound"]));
                return null;
            }

            return await Delete([reference], cancellationToken) ? reference : null;
        }

        private IQueryable<Reference> QueryWithDetails()
        {
            return DbContext.Set<Reference>()
                .AsNoTracking()
                .Include(item => item.Connector)
                .ThenInclude(item => item!.Integration)
                .ThenInclude(item => item!.IntegrationCategory);
        }
    }
}
