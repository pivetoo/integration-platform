using IntegrationPlatform.Domain.Entities;
using IntegrationPlatform.Domain.ValueObjects;
using IntegrationPlatform.Infrastructure.Services.ExecutionEngine;

namespace IntegrationPlatform.Testing.Infrastructure.ExecutionEngine
{
    [TestFixture]
    public sealed class IdempotencyKeyResolverTests
    {
        private static ProcessingQueue Item(string? key)
        {
            return new ProcessingQueue(1, 1, 1, ProcessingStatus.Pending, idempotencyKey: key);
        }

        [Test]
        public void Resolve_returns_explicit_key_when_payload_has_none()
        {
            IdempotencyKeyResolver.Resolve(Item("k-123"), []).Should().Be("k-123");
        }

        [Test]
        public void Resolve_falls_back_to_queue_id_when_no_key_anywhere()
        {
            ProcessingQueue item = Item(null);

            IdempotencyKeyResolver.Resolve(item, []).Should().Be($"q{item.Id}");
        }

        [TestCase("idempotencyKey")]
        [TestCase("IdempotencyKey")]
        public void Resolve_returns_null_when_payload_already_carries_the_key(string payloadField)
        {
            Dictionary<string, object> payload = new() { [payloadField] = "from-payload" };

            IdempotencyKeyResolver.Resolve(Item("k-123"), payload).Should().BeNull();
        }
    }
}
