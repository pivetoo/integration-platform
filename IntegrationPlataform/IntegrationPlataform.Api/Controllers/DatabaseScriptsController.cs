using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using IntegrationPlataform.Api.Contracts.DatabaseScripts;
using IntegrationPlataform.Application.Localization;
using IntegrationPlataform.Application.Requests.DatabaseScripts;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace IntegrationPlataform.Api.Controllers
{
    public sealed class DatabaseScriptsController : ApiControllerBase
    {
        private readonly IDatabaseScriptService databaseScriptService;
        private new IStringLocalizer<IntegrationPlataformResource> Localizer { get; }
        private static readonly Func<DatabaseScript, DatabaseScriptContract> MapDatabaseScript = DatabaseScriptContract.Projection.Compile();

        public DatabaseScriptsController(IDatabaseScriptService databaseScriptService, IStringLocalizer<IntegrationPlataformResource> localizer)
        {
            this.databaseScriptService = databaseScriptService;
            Localizer = localizer;
        }

        [RequireAccess]
        [GetEndpoint]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            PagedResult<DatabaseScript> result = await databaseScriptService.GetDatabaseScripts(request, cancellationToken);
            return Http200(new PagedResult<DatabaseScriptContract>
            {
                Items = result.Items.Select(MapDatabaseScript).ToArray(),
                Pagination = result.Pagination
            });
        }

        [RequireAccess]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            DatabaseScript? script = await databaseScriptService.GetDatabaseScriptById(id, cancellationToken);

            return script is null ? Http404(Localizer["database.script.notFound"]) : Http200(MapDatabaseScript(script));
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
            return Http201(MapDatabaseScript(script), Localizer["database.script.created"]);
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
            return Http200(MapDatabaseScript(script), Localizer["database.script.updated"]);
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

            return Http200(MapDatabaseScript(script), Localizer["database.script.deleted"]);
        }
    }
}
