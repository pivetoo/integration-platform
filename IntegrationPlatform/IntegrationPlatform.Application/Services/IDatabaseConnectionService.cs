using Archon.Core.Pagination;
using Archon.Application.Services;
using IntegrationPlatform.Application.Requests.DatabaseConnections;
using IntegrationPlatform.Domain.Entities;

namespace IntegrationPlatform.Application.Services
{
    public interface IDatabaseConnectionService : ICrudService<DatabaseConnection>
    {
        Task<PagedResult<DatabaseConnection>> GetDatabaseConnections(PagedRequest request, string? search, CancellationToken cancellationToken = default);

        Task<DatabaseConnection?> GetDatabaseConnectionById(long id, CancellationToken cancellationToken = default);

        Task<DatabaseConnection> CreateDatabaseConnection(CreateDatabaseConnectionRequest request, CancellationToken cancellationToken = default);

        Task<DatabaseConnection> UpdateDatabaseConnection(long id, UpdateDatabaseConnectionRequest request, CancellationToken cancellationToken = default);

        Task TestDatabaseConnection(CreateDatabaseConnectionRequest request, CancellationToken cancellationToken = default);
    }
}
