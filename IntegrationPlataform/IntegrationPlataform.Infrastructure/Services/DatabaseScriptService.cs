using Archon.Infrastructure.Services;
using IntegrationPlataform.Application.Requests.DatabaseScripts;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Infrastructure.Services
{
    public sealed class DatabaseScriptService : CrudService<DatabaseScript>, IDatabaseScriptService
    {
        public DatabaseScriptService(DbContext dbContext) : base(dbContext)
        {
        }

        public async Task<DatabaseScript> CreateDatabaseScript(CreateDatabaseScriptRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureDatabaseConnectionExists(request.DatabaseConnectionId, cancellationToken);

            DatabaseScript script = new(request.DatabaseConnectionId, request.Name, request.Script, request.Description);
            bool success = await Insert(cancellationToken, script);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return script;
        }

        public async Task<DatabaseScript> UpdateDatabaseScript(long id, UpdateDatabaseScriptRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException("Route id does not match body id.");
            }

            DatabaseScript? script = await DbContext.Set<DatabaseScript>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (script is null)
            {
                throw new InvalidOperationException("Database script not found.");
            }

            await EnsureDatabaseConnectionExists(request.DatabaseConnectionId, cancellationToken);

            script.Update(request.DatabaseConnectionId, request.Name, request.Script, request.Description);

            DatabaseScript? result = await Update(script, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return result;
        }

        private async Task EnsureDatabaseConnectionExists(long databaseConnectionId, CancellationToken cancellationToken)
        {
            bool exists = await DbContext.Set<DatabaseConnection>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == databaseConnectionId, cancellationToken);

            if (!exists)
            {
                throw new InvalidOperationException("Database connection not found.");
            }
        }
    }
}
