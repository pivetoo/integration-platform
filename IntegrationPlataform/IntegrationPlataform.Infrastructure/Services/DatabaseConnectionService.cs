using Archon.Infrastructure.Services;
using IntegrationPlataform.Application.Requests.DatabaseConnections;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using IntegrationPlataform.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Infrastructure.Services
{
    public sealed class DatabaseConnectionService : CrudService<DatabaseConnection>, IDatabaseConnectionService
    {
        public DatabaseConnectionService(DbContext dbContext) : base(dbContext)
        {
        }

        public async Task<DatabaseConnection> CreateDatabaseConnection(CreateDatabaseConnectionRequest request, CancellationToken cancellationToken = default)
        {
            DatabaseConnection connection = new(
                request.Name,
                NormalizeDatabaseType(request.Type),
                request.Host,
                request.Port,
                request.Database,
                request.Username,
                request.Password);

            bool success = await Insert(cancellationToken, connection);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return connection;
        }

        public async Task<DatabaseConnection> UpdateDatabaseConnection(long id, UpdateDatabaseConnectionRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException("Route id does not match body id.");
            }

            DatabaseConnection? connection = await DbContext.Set<DatabaseConnection>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (connection is null)
            {
                throw new InvalidOperationException("Database connection not found.");
            }

            connection.Update(
                request.Name,
                NormalizeDatabaseType(request.Type),
                request.Host,
                request.Port,
                request.Database,
                request.Username,
                request.Password);

            DatabaseConnection? result = await Update(connection, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return result;
        }

        private static DatabaseType NormalizeDatabaseType(int value)
        {
            int normalizedValue = Enum.IsDefined(typeof(DatabaseType), value)
                ? value
                : value + 1;

            if (!Enum.IsDefined(typeof(DatabaseType), normalizedValue))
            {
                throw new InvalidOperationException("Invalid database type.");
            }

            return (DatabaseType)normalizedValue;
        }
    }
}
