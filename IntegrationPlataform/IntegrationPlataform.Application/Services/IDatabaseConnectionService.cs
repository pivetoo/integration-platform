using Archon.Application.Services;
using IntegrationPlataform.Application.Requests.DatabaseConnections;
using IntegrationPlataform.Domain.Entities;

namespace IntegrationPlataform.Application.Services
{
    public interface IDatabaseConnectionService : ICrudService<DatabaseConnection>
    {
        Task<DatabaseConnection> CreateDatabaseConnection(CreateDatabaseConnectionRequest request, CancellationToken cancellationToken = default);

        Task<DatabaseConnection> UpdateDatabaseConnection(long id, UpdateDatabaseConnectionRequest request, CancellationToken cancellationToken = default);
    }
}
