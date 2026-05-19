using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using IntegrationPlatform.Api.Contracts.DatabaseConnections;
using IntegrationPlatform.Application.Localization;
using IntegrationPlatform.Application.Requests.DatabaseConnections;
using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace IntegrationPlatform.Api.Controllers
{
    public sealed class DatabaseConnectionsController : ApiControllerBase
    {
        private readonly IDatabaseConnectionService databaseConnectionService;
        private static readonly Func<DatabaseConnection, DatabaseConnectionContract> MapDatabaseConnection = DatabaseConnectionContract.Projection.Compile();
        private new IStringLocalizer<IntegrationPlatformResource> Localizer { get; }

        public DatabaseConnectionsController(IDatabaseConnectionService databaseConnectionService, IStringLocalizer<IntegrationPlatformResource> localizer)
        {
            this.databaseConnectionService = databaseConnectionService;
            Localizer = localizer;
        }

        [RequireAccess("Permite listar as conexões de banco de dados cadastradas na plataforma.")]
        [GetEndpoint]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, [FromQuery] string? search, CancellationToken cancellationToken)
        {
            PagedResult<DatabaseConnection> result = await databaseConnectionService.GetDatabaseConnections(request, search, cancellationToken);
            return Http200(new PagedResult<DatabaseConnectionContract>
            {
                Items = result.Items.Select(MapDatabaseConnection).ToArray(),
                Pagination = result.Pagination
            });
        }

        [RequireAccess("Permite consultar os detalhes de uma conexão de banco de dados específica.")]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            DatabaseConnection? connection = await databaseConnectionService.GetDatabaseConnectionById(id, cancellationToken);
            return connection is null ? Http404(Localizer["database.connection.notFound"]) : Http200(MapDatabaseConnection(connection));
        }

        [RequireAccess("Permite cadastrar uma nova conexão de banco de dados externo.")]
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

        [RequireAccess("Permite atualizar a configuração de uma conexão de banco de dados.")]
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

        [RequireAccess("Permite testar a conexão com um banco de dados externo antes do uso.")]
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
                return Http400(Localizer[exception.Message]);
            }
        }

        [RequireAccess("Permite excluir uma conexão de banco de dados cadastrada.")]
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
