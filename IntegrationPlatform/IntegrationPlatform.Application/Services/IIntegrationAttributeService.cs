using Archon.Core.Pagination;
using Archon.Application.Services;
using IntegrationPlatform.Domain.Entities;
using IntegrationPlatform.Domain.ValueObjects;

namespace IntegrationPlatform.Application.Services
{
    public interface IIntegrationAttributeService : ICrudService<IntegrationAttribute>
    {
        Task<PagedResult<IntegrationAttribute>> GetIntegrationAttributes(PagedRequest request, CancellationToken cancellationToken = default);

        Task<IntegrationAttribute?> GetIntegrationAttributeById(long id, CancellationToken cancellationToken = default);

        Task<List<IntegrationAttribute>> GetIntegrationAttributesByIntegration(long integrationId, bool includeHidden = false, CancellationToken cancellationToken = default);

        Task<IntegrationAttribute> UpdateIntegrationAttribute(
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
            CancellationToken cancellationToken = default);
    }
}
