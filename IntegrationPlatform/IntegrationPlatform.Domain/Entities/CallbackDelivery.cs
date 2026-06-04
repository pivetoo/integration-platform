using Archon.Core.Entities;
using IntegrationPlatform.Domain.ValueObjects;

namespace IntegrationPlatform.Domain.Entities
{
    // Outbox de callbacks de servico. Cada registro e uma entrega pendente para a CallbackUrl de um
    // conector apos uma execucao Success/Partial. Um BackgroundJob reprocessa as Pending com backoff.
    public class CallbackDelivery : Entity
    {
        public long ExecutionId { get; private set; }

        public long ConnectorId { get; private set; }

        public string ServiceIdentifier { get; private set; } = string.Empty;

        public string CallbackUrl { get; private set; } = string.Empty;

        public string? CallbackToken { get; private set; }

        public string Payload { get; private set; } = string.Empty;

        public CallbackDeliveryStatus Status { get; private set; }

        public int Attempts { get; private set; }

        public DateTimeOffset? NextAttemptAt { get; private set; }

        public DateTimeOffset? DeliveredAt { get; private set; }

        public string? LastError { get; private set; }

        private CallbackDelivery()
        {
        }

        public CallbackDelivery(long executionId, long connectorId, string serviceIdentifier, string callbackUrl, string? callbackToken, string payload, DateTimeOffset firstAttemptAt)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(callbackUrl);
            ArgumentException.ThrowIfNullOrWhiteSpace(payload);

            ExecutionId = executionId;
            ConnectorId = connectorId;
            ServiceIdentifier = serviceIdentifier;
            CallbackUrl = callbackUrl;
            CallbackToken = string.IsNullOrWhiteSpace(callbackToken) ? null : callbackToken;
            Payload = payload;
            Status = CallbackDeliveryStatus.Pending;
            Attempts = 0;
            NextAttemptAt = firstAttemptAt;
        }

        public void MarkDelivered(DateTimeOffset deliveredAt)
        {
            Attempts += 1;
            Status = CallbackDeliveryStatus.Delivered;
            DeliveredAt = deliveredAt;
            NextAttemptAt = null;
            LastError = null;
        }

        public void ScheduleRetry(DateTimeOffset nextAttemptAt, string error)
        {
            Attempts += 1;
            Status = CallbackDeliveryStatus.Pending;
            NextAttemptAt = nextAttemptAt;
            LastError = error;
        }

        public void MarkFailed(string error)
        {
            Attempts += 1;
            Status = CallbackDeliveryStatus.Failed;
            NextAttemptAt = null;
            LastError = error;
        }
    }
}
