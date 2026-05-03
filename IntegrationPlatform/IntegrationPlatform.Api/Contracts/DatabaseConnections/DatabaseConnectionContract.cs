using IntegrationPlatform.Domain.Entities;
using IntegrationPlatform.Domain.ValueObjects;
using System.Linq.Expressions;

namespace IntegrationPlatform.Api.Contracts.DatabaseConnections
{
    public sealed class DatabaseConnectionContract
    {
        public long Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public DatabaseType Type { get; init; }

        public string Host { get; init; } = string.Empty;

        public int Port { get; init; }

        public string Database { get; init; } = string.Empty;

        public string Username { get; init; } = string.Empty;

        public string Password { get; init; } = string.Empty;

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }

        public static Expression<Func<DatabaseConnection, DatabaseConnectionContract>> Projection => item => new DatabaseConnectionContract
        {
            Id = item.Id,
            Name = item.Name,
            Type = item.Type,
            Host = item.Host,
            Port = item.Port,
            Database = item.Database,
            Username = item.Username,
            Password = item.Password,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }
}
