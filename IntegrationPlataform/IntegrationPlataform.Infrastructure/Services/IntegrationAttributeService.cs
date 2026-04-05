using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using IntegrationPlataform.Application.Localization;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using IntegrationPlataform.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace IntegrationPlataform.Infrastructure.Services
{
    public sealed class IntegrationAttributeService : CrudService<IntegrationAttribute>, IIntegrationAttributeService
    {
        private readonly IStringLocalizer<IntegrationPlataformResource> Localizer;

        public IntegrationAttributeService(DbContext dbContext, IStringLocalizer<IntegrationPlataformResource> localizer) : base(dbContext)
        {
            Localizer = localizer;
        }

        public async Task<PagedResult<IntegrationAttribute>> GetIntegrationAttributes(PagedRequest request, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<IntegrationAttribute>()
                .AsNoTracking()
                .OrderBy(item => item.Order)
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<IntegrationAttribute?> GetIntegrationAttributeById(long id, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<IntegrationAttribute>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<List<IntegrationAttribute>> GetIntegrationAttributesByIntegration(long integrationId, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<IntegrationAttribute>()
                .AsNoTracking()
                .Where(item => item.IntegrationId == integrationId)
                .OrderBy(item => item.Order)
                .ToListAsync(cancellationToken);
        }

        public async Task<IntegrationAttribute> UpdateIntegrationAttribute(
            long id,
            long requestId,
            string field,
            string label,
            FieldType type,
            bool isRequired,
            int order,
            string? description,
            string? placeholder,
            string? defaultValue,
            string? group,
            bool isSensitive,
            CancellationToken cancellationToken = default)
        {
            if (id != requestId)
            {
                throw new InvalidOperationException(Localizer["request.route.idMismatch"]);
            }

            IntegrationAttribute? attribute = await DbContext.Set<IntegrationAttribute>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (attribute is null)
            {
                throw new InvalidOperationException(Localizer["integration.attribute.notFound"]);
            }

            attribute.Update(
                field,
                label,
                type,
                isRequired,
                order,
                description,
                placeholder,
                defaultValue,
                group,
                isSensitive);

            IntegrationAttribute? result = await Update(attribute, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetIntegrationAttributeById(result.Id, cancellationToken) ?? result;
        }

        public override async Task<IntegrationAttribute?> Delete(long id, CancellationToken cancellationToken = default)
        {
            MutableMessages.Clear();

            IntegrationAttribute? attribute = await GetIntegrationAttributeById(id, cancellationToken);

            if (attribute is null)
            {
                MutableMessages.Add(new KeyNotFoundException(Localizer["integration.attribute.notFound"]));
                return null;
            }

            return await Delete([attribute], cancellationToken) ? attribute : null;
        }
    }
}
