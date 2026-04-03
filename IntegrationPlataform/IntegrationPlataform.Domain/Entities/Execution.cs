using Archon.Core.Entities;
using IntegrationPlataform.Domain.ValueObjects;

namespace IntegrationPlataform.Domain.Entities
{
    public class Execution : Entity
    {
        private readonly List<ExecutionLog> logs = [];

        public ExecutionType Type { get; private set; }

        public long ConnectorId { get; private set; }

        public Connector Connector { get; private set; } = null!;

        public long? PipelineId { get; private set; }

        public Pipeline? Pipeline { get; private set; }

        public long? ProcessingQueueId { get; private set; }

        public ProcessingQueue? ProcessingQueue { get; private set; }

        public ExecutionStatus Status { get; private set; }

        public string? InputData { get; private set; }

        public string? OutputData { get; private set; }

        public string? Errors { get; private set; }

        public DateTimeOffset StartedAt { get; private set; }

        public DateTimeOffset? FinishedAt { get; private set; }

        public long? Duration { get; private set; }

        public IReadOnlyCollection<ExecutionLog> Logs => logs.AsReadOnly();

        private Execution()
        {
        }

        public Execution(ExecutionType type, long connectorId, ExecutionStatus status, DateTimeOffset startedAt, long? pipelineId = null, long? processingQueueId = null, string? inputData = null)
        {
            if (connectorId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(connectorId));
            }

            Type = type;
            ConnectorId = connectorId;
            PipelineId = pipelineId;
            ProcessingQueueId = processingQueueId;
            Status = status;
            InputData = inputData;
            StartedAt = startedAt;
        }

        public void MarkAsRunning()
        {
            Status = ExecutionStatus.Running;
            FinishedAt = null;
            Duration = null;
        }

        public void Complete(ExecutionStatus status, DateTimeOffset finishedAt, string? outputData, string? errors)
        {
            Status = status;
            FinishedAt = finishedAt;
            OutputData = outputData;
            Errors = errors;
            Duration = (long)(finishedAt - StartedAt).TotalMilliseconds;
        }

        public void UpdateInput(string? inputData)
        {
            InputData = inputData;
        }
    }
}
