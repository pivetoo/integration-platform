using IntegrationPlatform.Domain.Entities;
using IntegrationPlatform.Domain.ValueObjects;

namespace IntegrationPlatform.Testing.Domain.Entities
{
    [TestFixture]
    public sealed class CallbackDeliveryTests
    {
        private static readonly DateTimeOffset Now = new(2026, 6, 4, 12, 0, 0, TimeSpan.Zero);

        private static CallbackDelivery NewPending()
        {
            return new CallbackDelivery(executionId: 10, connectorId: 3, serviceIdentifier: "email.send", callbackUrl: "https://hook.example.com/cb", callbackToken: "tok", payload: "{}", firstAttemptAt: Now);
        }

        [Test]
        public void Constructor_creates_pending_delivery_with_zero_attempts()
        {
            CallbackDelivery delivery = NewPending();

            delivery.Status.Should().Be(CallbackDeliveryStatus.Pending);
            delivery.Attempts.Should().Be(0);
            delivery.NextAttemptAt.Should().Be(Now);
            delivery.ExecutionId.Should().Be(10);
            delivery.ConnectorId.Should().Be(3);
            delivery.ServiceIdentifier.Should().Be("email.send");
            delivery.CallbackUrl.Should().Be("https://hook.example.com/cb");
            delivery.CallbackToken.Should().Be("tok");
            delivery.Payload.Should().Be("{}");
            delivery.DeliveredAt.Should().BeNull();
            delivery.LastError.Should().BeNull();
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_normalizes_blank_token_to_null(string token)
        {
            CallbackDelivery delivery = new(1, 1, "svc", "https://x.com", token, "{}", Now);

            delivery.CallbackToken.Should().BeNull();
        }

        [TestCase("", "{}")]
        [TestCase("   ", "{}")]
        [TestCase("https://x.com", "")]
        [TestCase("https://x.com", "   ")]
        public void Constructor_with_blank_url_or_payload_throws(string url, string payload)
        {
            Action act = () => new CallbackDelivery(1, 1, "svc", url, null, payload, Now);

            act.Should().Throw<ArgumentException>();
        }

        [Test]
        public void MarkDelivered_transitions_to_delivered_and_clears_retry()
        {
            CallbackDelivery delivery = NewPending();

            delivery.MarkDelivered(Now.AddSeconds(5));

            delivery.Status.Should().Be(CallbackDeliveryStatus.Delivered);
            delivery.Attempts.Should().Be(1);
            delivery.DeliveredAt.Should().Be(Now.AddSeconds(5));
            delivery.NextAttemptAt.Should().BeNull();
            delivery.LastError.Should().BeNull();
        }

        [Test]
        public void ScheduleRetry_keeps_pending_and_records_next_attempt_and_error()
        {
            CallbackDelivery delivery = NewPending();

            delivery.ScheduleRetry(Now.AddMinutes(2), "HTTP 503");

            delivery.Status.Should().Be(CallbackDeliveryStatus.Pending);
            delivery.Attempts.Should().Be(1);
            delivery.NextAttemptAt.Should().Be(Now.AddMinutes(2));
            delivery.LastError.Should().Be("HTTP 503");
        }

        [Test]
        public void MarkFailed_transitions_to_failed_and_stops_retry()
        {
            CallbackDelivery delivery = NewPending();

            delivery.MarkFailed("HTTP 400");

            delivery.Status.Should().Be(CallbackDeliveryStatus.Failed);
            delivery.Attempts.Should().Be(1);
            delivery.NextAttemptAt.Should().BeNull();
            delivery.LastError.Should().Be("HTTP 400");
        }

        [Test]
        public void Attempts_accumulate_across_retries_until_delivered()
        {
            CallbackDelivery delivery = NewPending();

            delivery.ScheduleRetry(Now.AddMinutes(1), "HTTP 503");
            delivery.ScheduleRetry(Now.AddMinutes(2), "timeout");
            delivery.MarkDelivered(Now.AddMinutes(3));

            delivery.Attempts.Should().Be(3);
            delivery.Status.Should().Be(CallbackDeliveryStatus.Delivered);
        }
    }
}
