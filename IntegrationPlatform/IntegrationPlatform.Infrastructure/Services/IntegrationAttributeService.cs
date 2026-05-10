using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using IntegrationPlatform.Application.Localization;
using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using IntegrationPlatform.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace IntegrationPlatform.Infrastructure.Services
{
    public sealed class IntegrationAttributeService : CrudService<IntegrationAttribute>, IIntegrationAttributeService
    {
        private readonly IStringLocalizer<IntegrationPlatformResource> Localizer;

        public IntegrationAttributeService(DbContext dbContext, IStringLocalizer<IntegrationPlatformResource> localizer) : base(dbContext)
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

        public async Task<List<IntegrationAttribute>> GetIntegrationAttributesByIntegration(long integrationId, bool includeHidden = false, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<IntegrationAttribute>()
                .AsNoTracking()
                .Where(item => item.IntegrationId == integrationId && (includeHidden || !item.IsHidden))
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
            bool isHidden,
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
                isSensitive,
                isHidden);

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
