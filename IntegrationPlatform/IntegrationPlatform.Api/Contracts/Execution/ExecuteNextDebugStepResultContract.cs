using IntegrationPlatform.Application.Models;

namespace IntegrationPlatform.Api.Contracts.Execution
{
    public sealed class ExecuteNextDebugStepResultContract
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

        public static ExecuteNextDebugStepResultContract FromModel(ExecuteNextDebugStepResult result)
        {
            return new ExecuteNextDebugStepResultContract
            {
                DebugSessionId = result.DebugSessionId,
                ExecutionId = result.ExecutionId,
                ExecutedStep = result.ExecutedStep,
                ExecutedStepId = result.ExecutedStepId,
                ExecutedStepName = result.ExecutedStepName,
                Success = result.Success,
                InterruptedByError = result.InterruptedByError,
                FinishedFlow = result.FinishedFlow,
                RemainingSteps = result.RemainingSteps,
                NextStepId = result.NextStepId,
                NextStepName = result.NextStepName,
                Message = result.Message
            };
        }
    }
}
