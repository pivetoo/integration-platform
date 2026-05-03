using Archon.Core.Pagination;
using Archon.Application.Services;
using IntegrationPlatform.Application.Requests.DatabaseScripts;
using IntegrationPlatform.Domain.Entities;

namespace IntegrationPlatform.Application.Services
{
    public interface IDatabaseScriptService : ICrudService<DatabaseScript>
    {
        Task<PagedResult<DatabaseScript>> GetDatabaseScripts(PagedRequest request, CancellationToken cancellationToken = default);

        Task<DatabaseScript?> GetDatabaseScriptById(long id, CancellationToken cancellationToken = default);

        Task<DatabaseScript> CreateDatabaseScript(CreateDatabaseScriptRequest request, CancellationToken cancellationToken = default);

        Task<DatabaseScript> UpdateDatabaseScript(long id, UpdateDatabaseScriptRequest request, CancellationToken cancellationToken = default);
    }
}
