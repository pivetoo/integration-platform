using Archon.Core.Entities;

namespace IntegrationPlataform.Domain.Entities
{
    public class PipelineRoutine : Entity
    {
        public long ConnectorId { get; private set; }

        public Connector Connector { get; private set; } = null!;

        public long PipelineId { get; private set; }

        public Pipeline Pipeline { get; private set; } = null!;

        public bool IsActive { get; private set; }

        public int IntervalInMinutes { get; private set; }

        public string? DefaultPayload { get; private set; }

        public DateTimeOffset? LastExecutionAt { get; private set; }

        public DateTimeOffset? NextExecutionAt { get; private set; }

        private PipelineRoutine()
        {
        }

        public PipelineRoutine(long connectorId, long pipelineId, int intervalInMinutes, bool isActive = true, string? defaultPayload = null)
        {
            if (connectorId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(connectorId));
            }

            if (pipelineId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pipelineId));
            }

            if (intervalInMinutes <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(intervalInMinutes));
            }

            ConnectorId = connectorId;
            PipelineId = pipelineId;
            IntervalInMinutes = intervalInMinutes;
            IsActive = isActive;
            DefaultPayload = defaultPayload;
        }

        public void Update(int intervalInMinutes, bool isActive, string? defaultPayload, DateTimeOffset? nextExecutionAt)
        {
            if (intervalInMinutes <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(intervalInMinutes));
            }

            IntervalInMinutes = intervalInMinutes;
            IsActive = isActive;
            DefaultPayload = defaultPayload;
            NextExecutionAt = nextExecutionAt;
        }

        public void MarkExecution(DateTimeOffset executedAt, DateTimeOffset? nextExecutionAt)
        {
            LastExecutionAt = executedAt;
            NextExecutionAt = nextExecutionAt;
        }
    }
}
