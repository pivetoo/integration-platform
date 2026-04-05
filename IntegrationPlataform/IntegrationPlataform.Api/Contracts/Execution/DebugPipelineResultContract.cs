using IntegrationPlataform.Domain.ValueObjects;
using ExecutionEntity = IntegrationPlataform.Domain.Entities.Execution;

namespace IntegrationPlataform.Api.Contracts.Execution
{
    public sealed class DebugPipelineResultContract
    {
        public long Id { get; init; }

        public ExecutionStatus Status { get; init; }

        public ExecutionType Type { get; init; }

        public DateTimeOffset StartedAt { get; init; }

        public DateTimeOffset? FinishedAt { get; init; }

        public long? Duration { get; init; }

        public string? OutputData { get; init; }

        public string? Errors { get; init; }

        public static DebugPipelineResultContract FromExecution(ExecutionEntity execution)
        {
            return new DebugPipelineResultContract
            {
                Id = execution.Id,
                Status = execution.Status,
                Type = execution.Type,
                StartedAt = execution.StartedAt,
                FinishedAt = execution.FinishedAt,
                Duration = execution.Duration,
                OutputData = execution.OutputData,
                Errors = execution.Errors
            };
        }
    }
}
