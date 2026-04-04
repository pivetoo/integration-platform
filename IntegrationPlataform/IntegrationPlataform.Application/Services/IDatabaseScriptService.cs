using Archon.Application.Services;
using IntegrationPlataform.Application.Requests.DatabaseScripts;
using IntegrationPlataform.Domain.Entities;

namespace IntegrationPlataform.Application.Services
{
    public interface IDatabaseScriptService : ICrudService<DatabaseScript>
    {
        Task<DatabaseScript> CreateDatabaseScript(CreateDatabaseScriptRequest request, CancellationToken cancellationToken = default);

        Task<DatabaseScript> UpdateDatabaseScript(long id, UpdateDatabaseScriptRequest request, CancellationToken cancellationToken = default);
    }
}
