using Archon.Core.Pagination;
using Archon.Application.Services;
using IntegrationPlatform.Application.Requests.Connectors;
using IntegrationPlatform.Domain.Entities;

namespace IntegrationPlatform.Application.Services
{
    public interface IConnectorService : ICrudService<Connector>
    {
        Task<PagedResult<Connector>> GetConnectors(PagedRequest request, string? search, CancellationToken cancellationToken = default);

        Task<Connector?> GetConnectorById(long id, CancellationToken cancellationToken = default);

        Task<List<Connector>> GetConnectorsByIntegration(long integrationId, CancellationToken cancellationToken = default);

        Task<List<Connector>> GetActiveConnectors(CancellationToken cancellationToken = default);

        Task<List<Connector>> GetConnectorsByCategoryIdentifier(string identifier, CancellationToken cancellationToken = default);

        Task<Connector> CreateConnector(CreateConnectorRequest request, CancellationToken cancellationToken = default);

        Task<Connector> UpdateConnector(long id, UpdateConnectorRequest request, CancellationToken cancellationToken = default);
    }
}
