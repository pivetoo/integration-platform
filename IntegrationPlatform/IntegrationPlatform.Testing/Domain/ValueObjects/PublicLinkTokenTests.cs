using IntegrationPlatform.Domain.ValueObjects;

namespace IntegrationPlatform.Testing.Domain.ValueObjects
{
    [TestFixture]
    public sealed class PublicLinkTokenTests
    {
        [Test]
        public void Compose_should_prefix_tenant_with_separator()
        {
            PublicLinkToken.Compose("11d63d46", "abc123").Should().Be("11d63d46~abc123");
        }

        [Test]
        public void Compose_should_return_only_random_when_tenant_is_empty()
        {
            PublicLinkToken.Compose("", "abc123").Should().Be("abc123");
            PublicLinkToken.Compose(null, "abc123").Should().Be("abc123");
        }

        [Test]
        public void ExtractTenantId_should_return_prefix_before_separator()
        {
            PublicLinkToken.ExtractTenantId("11d63d46~abc123").Should().Be("11d63d46");
        }

        [Test]
        public void ExtractTenantId_should_return_null_when_no_separator()
        {
            PublicLinkToken.ExtractTenantId("abc123").Should().BeNull();
        }

        [Test]
        public void ExtractTenantId_should_return_null_for_empty_or_null()
        {
            PublicLinkToken.ExtractTenantId(null).Should().BeNull();
            PublicLinkToken.ExtractTenantId("").Should().BeNull();
        }

        [Test]
        public void ExtractTenantId_should_round_trip_with_compose()
        {
            string token = PublicLinkToken.Compose("tenant-x", "secret-y");
            PublicLinkToken.ExtractTenantId(token).Should().Be("tenant-x");
        }
    }
}
