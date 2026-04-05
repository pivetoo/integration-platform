using IntegrationPlataform.Domain.ValueObjects;

namespace IntegrationPlataform.Api.Contracts.Execution
{
    public sealed class StartDebugPipelineResultContract
    {
        public string DebugSessionId { get; init; } = string.Empty;

        public long ExecutionId { get; init; }

        public ExecutionStatus Status { get; init; }

        public int TotalSteps { get; init; }

        public int RemainingSteps { get; init; }

        public long? NextStepId { get; init; }

        public string? NextStepName { get; init; }
    }
}
