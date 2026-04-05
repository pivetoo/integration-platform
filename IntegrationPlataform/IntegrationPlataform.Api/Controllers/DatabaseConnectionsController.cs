using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using IntegrationPlataform.Api.Contracts.DatabaseConnections;
using IntegrationPlataform.Application.Localization;
using IntegrationPlataform.Application.Requests.DatabaseConnections;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace IntegrationPlataform.Api.Controllers
{
    public sealed class DatabaseConnectionsController : ApiControllerBase
    {
        private readonly IDatabaseConnectionService databaseConnectionService;
        private static readonly Func<DatabaseConnection, DatabaseConnectionContract> MapDatabaseConnection = DatabaseConnectionContract.Projection.Compile();
        private new IStringLocalizer<IntegrationPlataformResource> Localizer { get; }

        public DatabaseConnectionsController(IDatabaseConnectionService databaseConnectionService, IStringLocalizer<IntegrationPlataformResource> localizer)
        {
            this.databaseConnectionService = databaseConnectionService;
            Localizer = localizer;
        }

        [RequireAccess]
        [GetEndpoint]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            PagedResult<DatabaseConnection> result = await databaseConnectionService.GetDatabaseConnections(request, cancellationToken);
            return Http200(new PagedResult<DatabaseConnectionContract>
            {
                Items = result.Items.Select(MapDatabaseConnection).ToArray(),
                Pagination = result.Pagination
            });
        }

        [RequireAccess]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            DatabaseConnection? connection = await databaseConnectionService.GetDatabaseConnectionById(id, cancellationToken);
            return connection is null ? Http404(Localizer["database.connection.notFound"]) : Http200(MapDatabaseConnection(connection));
        }

        [RequireAccess]
        [PostEndpoint]
        public async Task<IActionResult> Create([FromBody] CreateDatabaseConnectionRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            DatabaseConnection connection = await databaseConnectionService.CreateDatabaseConnection(request, cancellationToken);
            return Http201(MapDatabaseConnection(connection), Localizer["database.connection.created"]);
        }

        [RequireAccess]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateDatabaseConnectionRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            DatabaseConnection connection = await databaseConnectionService.UpdateDatabaseConnection(id, request, cancellationToken);
            return Http200(MapDatabaseConnection(connection), Localizer["database.connection.updated"]);
        }

        [RequireAccess]
        [PostEndpoint("test")]
        public async Task<IActionResult> Test([FromBody] CreateDatabaseConnectionRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            try
            {
                await databaseConnectionService.TestDatabaseConnection(request, cancellationToken);
                return Http200(new { message = Localizer["database.connection.test.success"].Value });
            }
            catch (InvalidOperationException exception)
            {
                return Http400(exception.Message);
            }
        }

        [RequireAccess]
        [DeleteEndpoint("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            DatabaseConnection? connection = await databaseConnectionService.Delete(id, cancellationToken);
            if (connection is null)
            {
                return Http404(databaseConnectionService.GetErrorMessages());
            }

            return Http200(MapDatabaseConnection(connection), Localizer["database.connection.deleted"]);
        }
    }
}
