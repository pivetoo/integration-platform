using Archon.Core.Pagination;
using Archon.Application.Services;
using IntegrationPlataform.Domain.Entities;

namespace IntegrationPlataform.Application.Services
{
    public interface IConnectorAttributeValueService : ICrudService<ConnectorAttributeValue>
    {
        Task<PagedResult<ConnectorAttributeValue>> GetConnectorAttributeValues(PagedRequest request, CancellationToken cancellationToken = default);

        Task<ConnectorAttributeValue?> GetConnectorAttributeValueById(long id, CancellationToken cancellationToken = default);

        Task<List<ConnectorAttributeValue>> GetConnectorAttributeValuesByConnector(long connectorId, CancellationToken cancellationToken = default);

        Task<ConnectorAttributeValue> UpdateConnectorAttributeValue(long id, long requestId, long integrationAttributeId, string value, CancellationToken cancellationToken = default);
    }
}
