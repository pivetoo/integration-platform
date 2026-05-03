namespace IntegrationPlatform.Application.Models
{
    public sealed class ExecuteNextDebugStepResult
    {
        public string DebugSessionId { get; init; } = string.Empty;

        public long ExecutionId { get; init; }

        public bool ExecutedStep { get; init; }

        public long? ExecutedStepId { get; init; }

        public string? ExecutedStepName { get; init; }

        public bool? Success { get; init; }

        public bool? InterruptedByError { get; init; }

        public bool FinishedFlow { get; init; }

        public int RemainingSteps { get; init; }

        public long? NextStepId { get; init; }

        public string? NextStepName { get; init; }

        public string? Message { get; init; }
    }
}
