using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using IntegrationPlatform.Api.Contracts.DatabaseScripts;
using IntegrationPlatform.Application.Localization;
using IntegrationPlatform.Application.Requests.DatabaseScripts;
using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace IntegrationPlatform.Api.Controllers
{
    [AccessArea("databaseScripts.area")]
    public sealed class DatabaseScriptsController : ApiControllerBase
    {
        private readonly IDatabaseScriptService databaseScriptService;
        private new IStringLocalizer<IntegrationPlatformResource> Localizer { get; }
        private static readonly Func<DatabaseScript, DatabaseScriptContract> MapDatabaseScript = DatabaseScriptContract.Projection.Compile();

        public DatabaseScriptsController(IDatabaseScriptService databaseScriptService, IStringLocalizer<IntegrationPlatformResource> localizer)
        {
            this.databaseScriptService = databaseScriptService;
            Localizer = localizer;
        }

        [RequireAccess("databaseScripts.get.description")]
        [GetEndpoint]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, [FromQuery] string? search, CancellationToken cancellationToken)
        {
            PagedResult<DatabaseScript> result = await databaseScriptService.GetDatabaseScripts(request, search, cancellationToken);
            return Http200(new PagedResult<DatabaseScriptContract>
            {
                Items = result.Items.Select(MapDatabaseScript).ToArray(),
                Pagination = result.Pagination
            });
        }

        [RequireAccess("databaseScripts.getById.description")]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            DatabaseScript? script = await databaseScriptService.GetDatabaseScriptById(id, cancellationToken);

            return script is null ? Http404(Localizer["database.script.notFound"]) : Http200(MapDatabaseScript(script));
        }

        [RequireAccess("databaseScripts.create.description")]
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

        [RequireAccess("databaseScripts.update.description")]
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

        [RequireAccess("databaseScripts.delete.description")]
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
