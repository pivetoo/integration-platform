using IntegrationPlatform.Domain.Entities;

namespace IntegrationPlatform.Testing.Domain.Entities
{
    [TestFixture]
    public sealed class DatabaseScriptTests
    {
        [Test]
        public void Constructor_trims_name_and_sets_fields()
        {
            DatabaseScript script = new(databaseConnectionId: 3, name: "  Report  ", script: "SELECT 1", description: "  d  ");

            script.DatabaseConnectionId.Should().Be(3);
            script.Name.Should().Be("Report");
            script.Script.Should().Be("SELECT 1");
            script.Description.Should().Be("d");
        }

        [TestCase(0L)]
        [TestCase(-1L)]
        public void Constructor_with_invalid_connectionId_throws(long databaseConnectionId)
        {
            Action act = () => new DatabaseScript(databaseConnectionId, "name", "SELECT 1");

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [TestCase("", "SELECT 1")]
        [TestCase("   ", "SELECT 1")]
        [TestCase("name", "")]
        [TestCase("name", "   ")]
        public void Constructor_with_blank_name_or_script_throws(string name, string script)
        {
            Action act = () => new DatabaseScript(1, name, script);

            act.Should().Throw<ArgumentException>();
        }

        [Test]
        public void Update_changes_fields()
        {
            DatabaseScript script = new(1, "old", "SELECT 1");

            script.Update(2, "  new  ", "SELECT 2", "desc");

            script.DatabaseConnectionId.Should().Be(2);
            script.Name.Should().Be("new");
            script.Script.Should().Be("SELECT 2");
        }
    }
}
