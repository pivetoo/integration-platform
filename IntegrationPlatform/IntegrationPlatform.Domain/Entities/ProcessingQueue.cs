using Archon.Core.Entities;
using IntegrationPlatform.Domain.ValueObjects;

namespace IntegrationPlatform.Domain.Entities
{
    public class ProcessingQueue : Entity
    {
        private readonly List<Execution> executions = [];

        public long ConnectorId { get; private set; }

        public Connector Connector { get; private set; } = null!;

        public long PipelineId { get; private set; }

        public Pipeline Pipeline { get; private set; } = null!;

        public int Priority { get; private set; }

        public ProcessingStatus Status { get; private set; }

        public string? Payload { get; private set; }

        public string? LastError { get; private set; }

        public int Attempts { get; private set; }

        public string? IdempotencyKey { get; private set; }

        public DateTimeOffset? ScheduledAt { get; private set; }

        public DateTimeOffset? StartedAt { get; private set; }

        public DateTimeOffset? FinishedAt { get; private set; }

        public IReadOnlyCollection<Execution> Executions => executions.AsReadOnly();

        private ProcessingQueue()
        {
        }

        public ProcessingQueue(long connectorId, long pipelineId, int priority, ProcessingStatus status, string? payload = null, DateTimeOffset? scheduledAt = null, string? idempotencyKey = null)
        {
            if (connectorId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(connectorId));
            }

            if (pipelineId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pipelineId));
            }

            ConnectorId = connectorId;
            PipelineId = pipelineId;
            Priority = priority;
            Status = status;
            Payload = payload;
            ScheduledAt = scheduledAt;
            IdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey.Trim();
        }

        public void MarkAsProcessing(DateTimeOffset startedAt)
        {
            Status = ProcessingStatus.Processing;
            StartedAt = startedAt;
            FinishedAt = null;
            LastError = null;
        }

        public void Complete(DateTimeOffset finishedAt)
        {
            Status = ProcessingStatus.Completed;
            FinishedAt = finishedAt;
        }

        public void Fail(string error, DateTimeOffset finishedAt)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(error);

            Status = ProcessingStatus.Error;
            LastError = error.Trim();
            FinishedAt = finishedAt;
        }

        public void ScheduleRetry(DateTimeOffset nextAttemptAt, string error)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(error);

            Attempts += 1;
            Status = ProcessingStatus.Pending;
            ScheduledAt = nextAttemptAt;
            LastError = error.Trim();
            FinishedAt = null;
        }

        public void Cancel(DateTimeOffset finishedAt)
        {
            Status = ProcessingStatus.Cancelled;
            FinishedAt = finishedAt;
        }
    }
}
