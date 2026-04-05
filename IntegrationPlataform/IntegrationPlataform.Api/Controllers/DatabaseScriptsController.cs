using Archon.Api.Attributes;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using IntegrationPlataform.Api.Contracts.DatabaseScripts;
using IntegrationPlataform.Application.Requests.DatabaseScripts;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Api.Controllers
{
    public sealed class DatabaseScriptsController : IntegrationPlataformReadOnlyController<DatabaseScript>
    {
        private readonly IDatabaseScriptService databaseScriptService;

        public DatabaseScriptsController(DbContext dbContext, IDatabaseScriptService databaseScriptService) : base(dbContext)
        {
            this.databaseScriptService = databaseScriptService;
        }

        private IQueryable<DatabaseScriptContract> QueryContracts()
        {
            return DbContext.Set<DatabaseScript>()
                .AsNoTracking()
                .Select(DatabaseScriptContract.Projection);
        }

        [RequireAccess]
        [GetEndpoint]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            var result = await QueryContracts()
                .OrderBy(item => item.Name)
                .ToPagedResultAsync(request, cancellationToken);

            return Http200(result);
        }

        [RequireAccess]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            DatabaseScriptContract? script = await QueryContracts()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            return script is null ? Http404(Localizer["database.script.notFound"]) : Http200(script);
        }

        [RequireAccess]
        [PostEndpoint]
        public async Task<IActionResult> Create([FromBody] CreateDatabaseScriptRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            DatabaseScript script = await databaseScriptService.CreateDatabaseScript(request, cancellationToken);
            DatabaseScriptContract? contract = await QueryContracts()
                .FirstOrDefaultAsync(item => item.Id == script.Id, cancellationToken);

            return Http201(contract ?? DatabaseScriptContract.Projection.Compile()(script), Localizer["database.script.created"]);
        }

        [RequireAccess]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateDatabaseScriptRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            DatabaseScript script = await databaseScriptService.UpdateDatabaseScript(id, request, cancellationToken);
            DatabaseScriptContract? contract = await QueryContracts()
                .FirstOrDefaultAsync(item => item.Id == script.Id, cancellationToken);

            return Http200(contract ?? DatabaseScriptContract.Projection.Compile()(script), Localizer["database.script.updated"]);
        }

        [RequireAccess]
        [DeleteEndpoint("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            DatabaseScript? script = await databaseScriptService.Delete(id, cancellationToken);
            if (script is null)
            {
                return Http404(databaseScriptService.GetErrorMessages());
            }

            return Http200(script, Localizer["database.script.deleted"]);
        }
    }
}
