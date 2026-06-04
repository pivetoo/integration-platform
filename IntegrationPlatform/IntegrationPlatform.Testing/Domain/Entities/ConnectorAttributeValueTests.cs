using IntegrationPlatform.Domain.Entities;

namespace IntegrationPlatform.Testing.Domain.Entities
{
    [TestFixture]
    public sealed class ConnectorAttributeValueTests
    {
        [Test]
        public void Constructor_sets_fields()
        {
            ConnectorAttributeValue value = new(connectorId: 2, integrationAttributeId: 7, value: "secret-value");

            value.ConnectorId.Should().Be(2);
            value.IntegrationAttributeId.Should().Be(7);
            value.Value.Should().Be("secret-value");
        }

        [TestCase(0L, 7L)]
        [TestCase(-1L, 7L)]
        [TestCase(2L, 0L)]
        [TestCase(2L, -1L)]
        public void Constructor_with_invalid_references_throws(long connectorId, long integrationAttributeId)
        {
            Action act = () => new ConnectorAttributeValue(connectorId, integrationAttributeId, "v");

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_with_blank_value_throws(string value)
        {
            Action act = () => new ConnectorAttributeValue(1, 1, value);

            act.Should().Throw<ArgumentException>();
        }

        [Test]
        public void Update_changes_attribute_and_value()
        {
            ConnectorAttributeValue value = new(1, 1, "old");

            value.Update(9, "new");

            value.IntegrationAttributeId.Should().Be(9);
            value.Value.Should().Be("new");
        }
    }
}
