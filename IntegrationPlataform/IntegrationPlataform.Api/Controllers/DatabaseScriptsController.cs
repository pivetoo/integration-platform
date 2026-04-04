using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using IntegrationPlataform.Application.Requests.DatabaseScripts;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Api.Controllers
{
    public sealed class DatabaseScriptsController : ReadOnlyController<DatabaseScript>
    {
        private readonly IDatabaseScriptService databaseScriptService;

        public DatabaseScriptsController(DbContext dbContext, IDatabaseScriptService databaseScriptService) : base(dbContext)
        {
            this.databaseScriptService = databaseScriptService;
        }

        [RequireAccess]
        [GetEndpoint]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            return await base.Get(request, cancellationToken);
        }

        [RequireAccess]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            return await base.GetById(id, cancellationToken);
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
            return Http201(script, "Database script created successfully.");
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
            return Http200(script, "Database script updated successfully.");
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

            return Http200(script, "Database script deleted successfully.");
        }
    }
}
