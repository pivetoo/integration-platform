using IntegrationPlatform.Domain.Entities;

namespace IntegrationPlatform.Testing.Domain.Entities
{
    [TestFixture]
    public sealed class ConnectorTests
    {
        [Test]
        public void Constructor_trims_name_and_generates_webhook_token()
        {
            Connector connector = new(integrationId: 4, name: "  Santander  ", systemApplicationId: "  app-1  ");

            connector.IntegrationId.Should().Be(4);
            connector.Name.Should().Be("Santander");
            connector.SystemApplicationId.Should().Be("app-1");
            connector.IsActive.Should().BeTrue();
            connector.WebhookToken.Should().NotBeNullOrWhiteSpace();
            connector.WebhookToken!.Length.Should().Be(32);
            connector.WebhookToken.Should().NotContain("-");
        }

        [TestCase(0L)]
        [TestCase(-1L)]
        public void Constructor_with_invalid_integrationId_throws(long integrationId)
        {
            Action act = () => new Connector(integrationId, "name");

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_with_blank_name_throws(string name)
        {
            Action act = () => new Connector(1, name);

            act.Should().Throw<ArgumentException>();
        }

        [Test]
        public void EnsureWebhookToken_keeps_existing_token()
        {
            Connector connector = new(1, "c");
            string original = connector.WebhookToken!;

            connector.EnsureWebhookToken();

            connector.WebhookToken.Should().Be(original);
        }

        [Test]
        public void RegenerateWebhookToken_replaces_token()
        {
            Connector connector = new(1, "c");
            string original = connector.WebhookToken!;

            connector.RegenerateWebhookToken();

            connector.WebhookToken.Should().NotBe(original);
            connector.WebhookToken!.Length.Should().Be(32);
        }

        [Test]
        public void SetCallback_trims_values_and_normalizes_blank_to_null()
        {
            Connector connector = new(1, "c");

            connector.SetCallback("  https://cb.example.com  ", "   ");

            connector.CallbackUrl.Should().Be("https://cb.example.com");
            connector.CallbackToken.Should().BeNull();
        }

        [Test]
        public void Update_changes_fields_and_active_flag()
        {
            Connector connector = new(1, "old");

            connector.Update(2, "  new  ", "app", isActive: false);

            connector.IntegrationId.Should().Be(2);
            connector.Name.Should().Be("new");
            connector.SystemApplicationId.Should().Be("app");
            connector.IsActive.Should().BeFalse();
        }
    }
}
