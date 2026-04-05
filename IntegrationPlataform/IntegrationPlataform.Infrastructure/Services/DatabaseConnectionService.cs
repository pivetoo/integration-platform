using Archon.Infrastructure.Services;
using IntegrationPlataform.Application.Localization;
using IntegrationPlataform.Application.Requests.DatabaseConnections;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using IntegrationPlataform.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace IntegrationPlataform.Infrastructure.Services
{
    public sealed class DatabaseConnectionService : CrudService<DatabaseConnection>, IDatabaseConnectionService
    {
        private readonly IStringLocalizer<IntegrationPlataformResource> Localizer;

        public DatabaseConnectionService(DbContext dbContext, IStringLocalizer<IntegrationPlataformResource> localizer) : base(dbContext)
        {
            Localizer = localizer;
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
