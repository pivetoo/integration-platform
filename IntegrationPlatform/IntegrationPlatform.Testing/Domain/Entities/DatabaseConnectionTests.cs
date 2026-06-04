using IntegrationPlatform.Domain.Entities;
using IntegrationPlatform.Domain.ValueObjects;

namespace IntegrationPlatform.Testing.Domain.Entities
{
    [TestFixture]
    public sealed class DatabaseConnectionTests
    {
        private static DatabaseConnection NewConnection()
        {
            return new DatabaseConnection("  prod  ", DatabaseType.PostgreSql, "  db.host  ", 5432, "  app  ", "  user  ", "  pass  ");
        }

        [Test]
        public void Constructor_trims_and_sets_fields()
        {
            DatabaseConnection connection = NewConnection();

            connection.Name.Should().Be("prod");
            connection.Type.Should().Be(DatabaseType.PostgreSql);
            connection.Host.Should().Be("db.host");
            connection.Port.Should().Be(5432);
            connection.Database.Should().Be("app");
            connection.Username.Should().Be("user");
            connection.Password.Should().Be("pass");
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_with_invalid_port_throws(int port)
        {
            Action act = () => new DatabaseConnection("name", DatabaseType.PostgreSql, "host", port, "db", "user", "pass");

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [TestCase("", "host", "db", "user", "pass")]
        [TestCase("name", "  ", "db", "user", "pass")]
        [TestCase("name", "host", "", "user", "pass")]
        [TestCase("name", "host", "db", "   ", "pass")]
        [TestCase("name", "host", "db", "user", "")]
        public void Constructor_with_blank_required_field_throws(string name, string host, string database, string username, string password)
        {
            Action act = () => new DatabaseConnection(name, DatabaseType.PostgreSql, host, 5432, database, username, password);

            act.Should().Throw<ArgumentException>();
        }

        [Test]
        public void Update_changes_fields()
        {
            DatabaseConnection connection = NewConnection();

            connection.Update("other", DatabaseType.SqlServer, "host2", 1433, "db2", "user2", "pass2");

            connection.Name.Should().Be("other");
            connection.Type.Should().Be(DatabaseType.SqlServer);
            connection.Host.Should().Be("host2");
            connection.Port.Should().Be(1433);
            connection.Database.Should().Be("db2");
            connection.Username.Should().Be("user2");
        }
    }
}
