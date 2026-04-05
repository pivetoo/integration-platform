using Archon.Core.Pagination;
using Archon.Application.Services;
using IntegrationPlataform.Application.Requests.DatabaseConnections;
using IntegrationPlataform.Domain.Entities;

namespace IntegrationPlataform.Application.Services
{
    public interface IDatabaseConnectionService : ICrudService<DatabaseConnection>
    {
        Task<PagedResult<DatabaseConnection>> GetDatabaseConnections(PagedRequest request, CancellationToken cancellationToken = default);

        Task<DatabaseConnection?> GetDatabaseConnectionById(long id, CancellationToken cancellationToken = default);

        Task<DatabaseConnection> CreateDatabaseConnection(CreateDatabaseConnectionRequest request, CancellationToken cancellationToken = default);

        Task<DatabaseConnection> UpdateDatabaseConnection(long id, UpdateDatabaseConnectionRequest request, CancellationToken cancellationToken = default);

        Task TestDatabaseConnection(CreateDatabaseConnectionRequest request, CancellationToken cancellationToken = default);
    }
}
