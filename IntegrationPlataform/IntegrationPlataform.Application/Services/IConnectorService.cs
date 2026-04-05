using Archon.Core.Pagination;
using Archon.Application.Services;
using IntegrationPlataform.Application.Requests.Connectors;
using IntegrationPlataform.Domain.Entities;

namespace IntegrationPlataform.Application.Services
{
    public interface IConnectorService : ICrudService<Connector>
    {
        Task<PagedResult<Connector>> GetConnectors(PagedRequest request, CancellationToken cancellationToken = default);

        Task<Connector?> GetConnectorById(long id, CancellationToken cancellationToken = default);

        Task<List<Connector>> GetConnectorsByIntegration(long integrationId, CancellationToken cancellationToken = default);

        Task<List<Connector>> GetActiveConnectors(CancellationToken cancellationToken = default);

        Task<Connector> CreateConnector(CreateConnectorRequest request, CancellationToken cancellationToken = default);

        Task<Connector> UpdateConnector(long id, UpdateConnectorRequest request, CancellationToken cancellationToken = default);
    }
}
