using IntegrationPlatform.Infrastructure.Security;

namespace IntegrationPlatform.Testing.Infrastructure.Security
{
    [TestFixture]
    public sealed class WebhookAuthTokenGeneratorTests
    {
        [TestCase("AAAAAB", 4, true)]
        [TestCase("AAAAB", 4, false)]
        [TestCase("AAAA", 4, false)]
        [TestCase("ABABABAB", 4, false)]
        [TestCase("12FFFFF3", 4, true)]
        [TestCase("", 4, false)]
        public void HasIdenticalRunLongerThan_detects_runs(string value, int maxRun, bool expected)
        {
            WebhookAuthTokenGenerator.HasIdenticalRunLongerThan(value, maxRun).Should().Be(expected);
        }

        [Test]
        public void Generate_returns_64_hex_chars_without_runs_longer_than_four()
        {
            for (int index = 0; index < 500; index++)
            {
                string token = WebhookAuthTokenGenerator.Generate();

                token.Should().HaveLength(64);
                token.Should().MatchRegex("^[0-9A-F]{64}$");
                WebhookAuthTokenGenerator.HasIdenticalRunLongerThan(token, WebhookAuthTokenGenerator.MaxIdenticalRun).Should().BeFalse();
            }
        }
    }
}
