using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using IntegrationPlataform.Application.Localization;
using IntegrationPlataform.Application.Requests.DatabaseConnections;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using IntegrationPlataform.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace IntegrationPlataform.Infrastructure.Services
{
    public sealed class DatabaseConnectionService : CrudService<DatabaseConnection>, IDatabaseConnectionService
    {
        private readonly IStringLocalizer<IntegrationPlataformResource> Localizer;

        public DatabaseConnectionService(DbContext dbContext, IStringLocalizer<IntegrationPlataformResource> localizer) : base(dbContext)
        {
            Localizer = localizer;
        }

        public async Task<PagedResult<DatabaseConnection>> GetDatabaseConnections(PagedRequest request, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<DatabaseConnection>()
                .AsNoTracking()
                .OrderBy(item => item.Name)
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<DatabaseConnection?> GetDatabaseConnectionById(long id, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<DatabaseConnection>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
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
                throw new InvalidOperationException(Localizer["request.route.idMismatch"]);
            }

            DatabaseConnection? connection = await DbContext.Set<DatabaseConnection>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (connection is null)
            {
                throw new InvalidOperationException(Localizer["database.connection.notFound"]);
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

        public async Task TestDatabaseConnection(CreateDatabaseConnectionRequest request, CancellationToken cancellationToken = default)
        {
            DatabaseType databaseType = NormalizeDatabaseType(request.Type);
            string connectionString = BuildConnectionString(request, databaseType);

            try
            {
                switch (databaseType)
                {
                    case DatabaseType.PostgreSql:
                    {
                        await using NpgsqlConnection connection = new(connectionString);
                        await connection.OpenAsync(cancellationToken);
                        break;
                    }
                    case DatabaseType.SqlServer:
                    {
                        await using SqlConnection connection = new(connectionString);
                        await connection.OpenAsync(cancellationToken);
                        break;
                    }
                    default:
                        throw new InvalidOperationException(Localizer["database.connection.test.unsupportedType", databaseType]);
                }
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(Localizer["database.connection.test.failed", ex.Message]);
            }
        }

        private DatabaseType NormalizeDatabaseType(int value)
        {
            int normalizedValue = Enum.IsDefined(typeof(DatabaseType), value)
                ? value
                : value + 1;

            if (!Enum.IsDefined(typeof(DatabaseType), normalizedValue))
            {
                throw new InvalidOperationException(Localizer["database.connection.type.invalid"]);
            }

            return (DatabaseType)normalizedValue;
        }

        private string BuildConnectionString(CreateDatabaseConnectionRequest request, DatabaseType databaseType)
        {
            return databaseType switch
            {
                DatabaseType.PostgreSql =>
                    $"Host={request.Host};Port={request.Port};Database={request.Database};Username={request.Username};Password={request.Password}",
                DatabaseType.SqlServer =>
                    $"Server={request.Host},{request.Port};Database={request.Database};User Id={request.Username};Password={request.Password};TrustServerCertificate=true;Encrypt=false",
                _ => throw new InvalidOperationException(Localizer["database.connection.type.unsupported"])
            };
        }

        public override async Task<DatabaseConnection?> Delete(long id, CancellationToken cancellationToken = default)
        {
            MutableMessages.Clear();

            DatabaseConnection? connection = await DbContext.Set<DatabaseConnection>()
                .AsNoTracking()
                .FirstOrDefaultAsync(current => current.Id == id, cancellationToken);

            if (connection is null)
            {
                MutableMessages.Add(new KeyNotFoundException(Localizer["database.connection.notFound"]));
                return null;
            }

            return await Delete([connection], cancellationToken) ? connection : null;
        }
    }
}
