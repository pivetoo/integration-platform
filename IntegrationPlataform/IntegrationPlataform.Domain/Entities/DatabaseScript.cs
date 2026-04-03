using Archon.Core.Entities;

namespace IntegrationPlataform.Domain.Entities
{
    public class DatabaseScript : Entity
    {
        public long DatabaseConnectionId { get; private set; }

        public DatabaseConnection DatabaseConnection { get; private set; } = null!;

        public string Name { get; private set; } = string.Empty;

        public string? Description { get; private set; }

        public string Script { get; private set; } = string.Empty;

        private DatabaseScript()
        {
        }

        public DatabaseScript(long databaseConnectionId, string name, string script, string? description = null)
        {
            if (databaseConnectionId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(databaseConnectionId));
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(script);

            DatabaseConnectionId = databaseConnectionId;
            Name = name.Trim();
            Description = description?.Trim();
            Script = script;
        }

        public void Update(long databaseConnectionId, string name, string script, string? description)
        {
            if (databaseConnectionId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(databaseConnectionId));
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(script);

            DatabaseConnectionId = databaseConnectionId;
            Name = name.Trim();
            Description = description?.Trim();
            Script = script;
        }
    }
}
