using Archon.Core.Entities;
using IntegrationPlataform.Domain.ValueObjects;

namespace IntegrationPlataform.Domain.Entities
{
    public class DatabaseConnection : Entity
    {
        private readonly List<DatabaseScript> scripts = [];

        public string Name { get; private set; } = string.Empty;

        public DatabaseType Type { get; private set; }

        public string Host { get; private set; } = string.Empty;

        public int Port { get; private set; }

        public string Database { get; private set; } = string.Empty;

        public string Username { get; private set; } = string.Empty;

        public string Password { get; private set; } = string.Empty;

        public IReadOnlyCollection<DatabaseScript> Scripts => scripts.AsReadOnly();

        private DatabaseConnection()
        {
        }

        public DatabaseConnection(string name, DatabaseType type, string host, int port, string database, string username, string password)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(host);
            ArgumentException.ThrowIfNullOrWhiteSpace(database);
            ArgumentException.ThrowIfNullOrWhiteSpace(username);
            ArgumentException.ThrowIfNullOrWhiteSpace(password);

            if (port <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }

            Name = name.Trim();
            Type = type;
            Host = host.Trim();
            Port = port;
            Database = database.Trim();
            Username = username.Trim();
            Password = password.Trim();
        }

        public void Update(string name, DatabaseType type, string host, int port, string database, string username, string password)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(host);
            ArgumentException.ThrowIfNullOrWhiteSpace(database);
            ArgumentException.ThrowIfNullOrWhiteSpace(username);
            ArgumentException.ThrowIfNullOrWhiteSpace(password);

            if (port <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }

            Name = name.Trim();
            Type = type;
            Host = host.Trim();
            Port = port;
            Database = database.Trim();
            Username = username.Trim();
            Password = password.Trim();
        }
    }
}
