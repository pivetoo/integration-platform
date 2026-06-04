using IntegrationPlatform.Domain.Entities;

namespace IntegrationPlatform.Testing.Domain.Entities
{
    [TestFixture]
    public sealed class IntegrationCategoryTests
    {
        [Test]
        public void Constructor_normalizes_identifier_and_trims_name()
        {
            IntegrationCategory category = new("  Payment  ", "  Pagamentos  ", "  d  ");

            category.Identifier.Should().Be("payment");
            category.Name.Should().Be("Pagamentos");
            category.Description.Should().Be("d");
            category.IsSystem.Should().BeFalse();
            category.IsActive.Should().BeTrue();
        }

        [TestCase("", "name")]
        [TestCase("id", "   ")]
        public void Constructor_with_blank_identifier_or_name_throws(string identifier, string name)
        {
            Action act = () => new IntegrationCategory(identifier, name);

            act.Should().Throw<ArgumentException>();
        }

        [Test]
        public void Update_on_system_category_changing_identifier_throws()
        {
            IntegrationCategory category = new("payment", "Payment", isSystem: true);

            Action act = () => category.Update("changed", "Payment", null, true);

            act.Should().Throw<InvalidOperationException>();
        }

        [Test]
        public void Update_on_system_category_keeping_identifier_succeeds()
        {
            IntegrationCategory category = new("payment", "Payment", isSystem: true);

            category.Update("Payment", "Renamed", "desc", false);

            category.Name.Should().Be("Renamed");
            category.IsActive.Should().BeFalse();
        }

        [Test]
        public void Update_on_non_system_category_can_change_identifier()
        {
            IntegrationCategory category = new("old", "Cat");

            category.Update("new", "Cat", null, true);

            category.Identifier.Should().Be("new");
        }
    }
}
