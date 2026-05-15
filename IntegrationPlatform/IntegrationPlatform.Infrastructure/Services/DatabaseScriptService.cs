using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using IntegrationPlatform.Application.Localization;
using IntegrationPlatform.Application.Requests.DatabaseScripts;
using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace IntegrationPlatform.Infrastructure.Services
{
    public sealed class DatabaseScriptService : CrudService<DatabaseScript>, IDatabaseScriptService
    {
        private readonly IStringLocalizer<IntegrationPlatformResource> Localizer;

        public DatabaseScriptService(DbContext dbContext, IStringLocalizer<IntegrationPlatformResource> localizer) : base(dbContext)
        {
            Localizer = localizer;
        }

        public async Task<PagedResult<DatabaseScript>> GetDatabaseScripts(PagedRequest request, string? search, CancellationToken cancellationToken = default)
        {
            var query = QueryWithDetails();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var lower = search.ToLower();
                query = query.Where(item => item.Name.ToLower().Contains(lower) || (item.Description != null && item.Description.ToLower().Contains(lower)));
            }
            return await query
                .OrderBy(item => item.Name)
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<DatabaseScript?> GetDatabaseScriptById(long id, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
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

            return await GetDatabaseScriptById(script.Id, cancellationToken) ?? script;
        }

        public async Task<DatabaseScript> UpdateDatabaseScript(long id, UpdateDatabaseScriptRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException(Localizer["request.route.idMismatch"]);
            }

            DatabaseScript? script = await DbContext.Set<DatabaseScript>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (script is null)
            {
                throw new InvalidOperationException(Localizer["database.script.notFound"]);
            }

            await EnsureDatabaseConnectionExists(request.DatabaseConnectionId, cancellationToken);

            script.Update(request.DatabaseConnectionId, request.Name, request.Script, request.Description);

            DatabaseScript? result = await Update(script, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetDatabaseScriptById(result.Id, cancellationToken) ?? result;
        }

        private async Task EnsureDatabaseConnectionExists(long databaseConnectionId, CancellationToken cancellationToken)
        {
            bool exists = await DbContext.Set<DatabaseConnection>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == databaseConnectionId, cancellationToken);

            if (!exists)
            {
                throw new InvalidOperationException(Localizer["database.connection.notFound"]);
            }
        }

        public override async Task<DatabaseScript?> Delete(long id, CancellationToken cancellationToken = default)
        {
            MutableMessages.Clear();

            DatabaseScript? script = await GetDatabaseScriptById(id, cancellationToken);

            if (script is null)
            {
                MutableMessages.Add(new KeyNotFoundException(Localizer["database.script.notFound"]));
                return null;
            }

            return await Delete([script], cancellationToken) ? script : null;
        }

        private IQueryable<DatabaseScript> QueryWithDetails()
        {
            return DbContext.Set<DatabaseScript>()
                .AsNoTracking()
                .Include(item => item.DatabaseConnection);
        }
    }
}
