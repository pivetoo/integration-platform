using IntegrationPlatform.Domain.Entities;
using IntegrationPlatform.Domain.ValueObjects;

namespace IntegrationPlatform.Testing.Domain.Entities
{
    [TestFixture]
    public sealed class IntegrationAttributeTests
    {
        [Test]
        public void Constructor_trims_and_sets_fields()
        {
            IntegrationAttribute attribute = new(integrationId: 6, field: "  apiKey  ", label: "  API Key  ", FieldType.Text, isRequired: true, order: 2, group: "  Auth  ", isSensitive: true, isHidden: false);

            attribute.IntegrationId.Should().Be(6);
            attribute.Field.Should().Be("apiKey");
            attribute.Label.Should().Be("API Key");
            attribute.Type.Should().Be(FieldType.Text);
            attribute.IsRequired.Should().BeTrue();
            attribute.Order.Should().Be(2);
            attribute.Group.Should().Be("Auth");
            attribute.IsSensitive.Should().BeTrue();
            attribute.IsHidden.Should().BeFalse();
        }

        [TestCase(0L)]
        [TestCase(-1L)]
        public void Constructor_with_invalid_integrationId_throws(long integrationId)
        {
            Action act = () => new IntegrationAttribute(integrationId, "field", "label", FieldType.Text, false, 1);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [TestCase("", "label")]
        [TestCase("   ", "label")]
        [TestCase("field", "")]
        [TestCase("field", "   ")]
        public void Constructor_with_blank_field_or_label_throws(string field, string label)
        {
            Action act = () => new IntegrationAttribute(1, field, label, FieldType.Text, false, 1);

            act.Should().Throw<ArgumentException>();
        }

        [Test]
        public void Update_changes_fields()
        {
            IntegrationAttribute attribute = new(1, "field", "label", FieldType.Text, false, 1);

            attribute.Update("  secret  ", "  Secret  ", FieldType.LongText, isRequired: true, order: 5, description: "d", placeholder: "p", defaultValue: "x", group: "g", isSensitive: true, isHidden: true);

            attribute.Field.Should().Be("secret");
            attribute.Label.Should().Be("Secret");
            attribute.Type.Should().Be(FieldType.LongText);
            attribute.IsRequired.Should().BeTrue();
            attribute.Order.Should().Be(5);
            attribute.IsSensitive.Should().BeTrue();
            attribute.IsHidden.Should().BeTrue();
        }
    }
}
