using IntegrationPlatform.Infrastructure.Services.ExecutionEngine;

namespace IntegrationPlatform.Testing.Infrastructure.ExecutionEngine
{
    [TestFixture]
    public sealed class RetryPolicyTests
    {
        [TestCase(0, 30)]
        [TestCase(1, 60)]
        [TestCase(2, 120)]
        [TestCase(3, 240)]
        [TestCase(4, 480)]
        public void NextRetryDelay_returns_exponential_backoff(int attempts, double expectedSeconds)
        {
            TimeSpan? delay = RetryPolicy.NextRetryDelay(attempts, maxAttempts: 6, baseBackoffSeconds: 30);

            delay.Should().Be(TimeSpan.FromSeconds(expectedSeconds));
        }

        [TestCase(5)]
        [TestCase(6)]
        [TestCase(10)]
        public void NextRetryDelay_returns_null_when_max_reached(int attempts)
        {
            RetryPolicy.NextRetryDelay(attempts, maxAttempts: 6, baseBackoffSeconds: 30).Should().BeNull();
        }

        [Test]
        public void NextRetryDelay_with_max_one_fails_immediately()
        {
            RetryPolicy.NextRetryDelay(0, maxAttempts: 1, baseBackoffSeconds: 30).Should().BeNull();
        }
    }
}
