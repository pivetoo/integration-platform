using IntegrationPlatform.Api.Contracts.DatabaseConnections;
using IntegrationPlatform.Domain.Entities;
using System.Linq.Expressions;

namespace IntegrationPlatform.Api.Contracts.DatabaseScripts
{
    public sealed class DatabaseScriptContract
    {
        public long Id { get; init; }

        public long DatabaseConnectionId { get; init; }

        public string Name { get; init; } = string.Empty;

        public string? Description { get; init; }

        public string Script { get; init; } = string.Empty;

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }

        public DatabaseConnectionContract? DatabaseConnection { get; init; }

        public static Expression<Func<DatabaseScript, DatabaseScriptContract>> Projection => item => new DatabaseScriptContract
        {
            Id = item.Id,
            DatabaseConnectionId = item.DatabaseConnectionId,
            Name = item.Name,
            Description = item.Description,
            Script = item.Script,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt,
            DatabaseConnection = item.DatabaseConnection == null
                ? null
                : new DatabaseConnectionContract
                {
                    Id = item.DatabaseConnection.Id,
                    Name = item.DatabaseConnection.Name,
                    Type = item.DatabaseConnection.Type,
                    Host = item.DatabaseConnection.Host,
                    Port = item.DatabaseConnection.Port,
                    Database = item.DatabaseConnection.Database,
                    Username = item.DatabaseConnection.Username,
                    Password = item.DatabaseConnection.Password,
                    CreatedAt = item.DatabaseConnection.CreatedAt,
                    UpdatedAt = item.DatabaseConnection.UpdatedAt
                }
        };
    }

}
