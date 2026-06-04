using IntegrationPlatform.Domain.Entities;

namespace IntegrationPlatform.Testing.Domain.Entities
{
    [TestFixture]
    public sealed class IntegrationTests
    {
        [Test]
        public void Constructor_trims_and_sets_fields()
        {
            Integration integration = new(identifier: "  santander  ", name: "  Santander  ", description: "  bank  ", integrationCategoryId: 3, iconUrl: "  https://i/x.png  ");

            integration.Identifier.Should().Be("santander");
            integration.Name.Should().Be("Santander");
            integration.Description.Should().Be("bank");
            integration.IntegrationCategoryId.Should().Be(3);
            integration.IconUrl.Should().Be("https://i/x.png");
            integration.IsActive.Should().BeTrue();
        }

        [TestCase("", "name")]
        [TestCase("   ", "name")]
        [TestCase("id", "")]
        [TestCase("id", "   ")]
        public void Constructor_with_blank_identifier_or_name_throws(string identifier, string name)
        {
            Action act = () => new Integration(identifier, name);

            act.Should().Throw<ArgumentException>();
        }

        [Test]
        public void Update_changes_fields_active_and_webhook_support()
        {
            Integration integration = new("old", "Old");

            integration.Update("  new  ", "  New  ", "desc", integrationCategoryId: 9, isActive: false, iconUrl: "  i  ", supportsWebhook: true);

            integration.Identifier.Should().Be("new");
            integration.Name.Should().Be("New");
            integration.IntegrationCategoryId.Should().Be(9);
            integration.IsActive.Should().BeFalse();
            integration.SupportsWebhook.Should().BeTrue();
        }
    }
}
