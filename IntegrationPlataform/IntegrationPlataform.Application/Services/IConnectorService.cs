using Archon.Application.Services;
using IntegrationPlataform.Application.Requests.Connectors;
using IntegrationPlataform.Domain.Entities;

namespace IntegrationPlataform.Application.Services
{
    public interface IConnectorService : ICrudService<Connector>
    {
        Task<Connector> CreateConnector(CreateConnectorRequest request, CancellationToken cancellationToken = default);

        Task<Connector> UpdateConnector(long id, UpdateConnectorRequest request, CancellationToken cancellationToken = default);
    }
}
