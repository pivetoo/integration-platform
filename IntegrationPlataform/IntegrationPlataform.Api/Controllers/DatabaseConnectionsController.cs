using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using IntegrationPlataform.Application.Requests.DatabaseConnections;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using IntegrationPlataform.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace IntegrationPlataform.Api.Controllers
{
    public sealed class DatabaseConnectionsController : ReadOnlyController<DatabaseConnection>
    {
        private readonly IDatabaseConnectionService databaseConnectionService;

        public DatabaseConnectionsController(DbContext dbContext, IDatabaseConnectionService databaseConnectionService) : base(dbContext)
        {
            this.databaseConnectionService = databaseConnectionService;
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
        public async Task<IActionResult> Create([FromBody] CreateDatabaseConnectionRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            DatabaseConnection connection = await databaseConnectionService.CreateDatabaseConnection(request, cancellationToken);
            return Http201(connection, "Database connection created successfully.");
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
            return Http200(connection, "Database connection updated successfully.");
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
                DatabaseType databaseType = NormalizeDatabaseType(request.Type);
                string connectionString = BuildConnectionString(request, databaseType);

                switch (databaseType)
                {
                    case DatabaseType.PostgreSql:
                    {
                        await using NpgsqlConnection connection = new(connectionString);
                        await connection.OpenAsync(cancellationToken);
                        break;
                    }
                    case DatabaseType.SqlServer:
                    {
                        await using SqlConnection connection = new(connectionString);
                        await connection.OpenAsync(cancellationToken);
                        break;
                    }
                    default:
                        return Http400($"Database type {databaseType} is not supported for connection tests.");
                }

                return Http200(new { message = "Database connection tested successfully." });
            }
            catch (Exception ex)
            {
                return Http400($"Error testing database connection: {ex.Message}");
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

            return Http200(connection, "Database connection deleted successfully.");
        }

        private static DatabaseType NormalizeDatabaseType(int value)
        {
            int normalizedValue = Enum.IsDefined(typeof(DatabaseType), value)
                ? value
                : value + 1;

            if (!Enum.IsDefined(typeof(DatabaseType), normalizedValue))
            {
                throw new InvalidOperationException("Invalid database type.");
            }

            return (DatabaseType)normalizedValue;
        }

        private static string BuildConnectionString(CreateDatabaseConnectionRequest request, DatabaseType databaseType)
        {
            return databaseType switch
            {
                DatabaseType.PostgreSql =>
                    $"Host={request.Host};Port={request.Port};Database={request.Database};Username={request.Username};Password={request.Password}",
                DatabaseType.SqlServer =>
                    $"Server={request.Host},{request.Port};Database={request.Database};User Id={request.Username};Password={request.Password};TrustServerCertificate=true;Encrypt=false",
                _ => throw new InvalidOperationException("Unsupported database type.")
            };
        }
    }
}
