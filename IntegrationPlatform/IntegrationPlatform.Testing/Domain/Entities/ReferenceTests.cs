using IntegrationPlatform.Domain.Entities;

namespace IntegrationPlatform.Testing.Domain.Entities
{
    [TestFixture]
    public sealed class ReferenceTests
    {
        [Test]
        public void Constructor_trims_and_sets_fields()
        {
            Reference reference = new(connectorId: 2, entityName: "  Customer  ", internalId: "  10  ", externalId: "  ext-99  ");

            reference.ConnectorId.Should().Be(2);
            reference.EntityName.Should().Be("Customer");
            reference.InternalId.Should().Be("10");
            reference.ExternalId.Should().Be("ext-99");
        }

        [TestCase(0L)]
        [TestCase(-1L)]
        public void Constructor_with_invalid_connectorId_throws(long connectorId)
        {
            Action act = () => new Reference(connectorId, "entity", "1", "2");

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [TestCase("", "1", "2")]
        [TestCase("entity", "   ", "2")]
        [TestCase("entity", "1", "")]
        public void Constructor_with_blank_field_throws(string entityName, string internalId, string externalId)
        {
            Action act = () => new Reference(1, entityName, internalId, externalId);

            act.Should().Throw<ArgumentException>();
        }

        [Test]
        public void Update_changes_fields()
        {
            Reference reference = new(1, "Customer", "1", "a");

            reference.Update("  Order  ", "  2  ", "  b  ");

            reference.EntityName.Should().Be("Order");
            reference.InternalId.Should().Be("2");
            reference.ExternalId.Should().Be("b");
        }
    }
}
