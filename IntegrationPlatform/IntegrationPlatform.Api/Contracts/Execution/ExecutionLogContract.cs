using IntegrationPlatform.Domain.Entities;
using IntegrationPlatform.Domain.ValueObjects;
using System.Linq.Expressions;

namespace IntegrationPlatform.Api.Contracts.Execution
{
    public sealed class ExecutionLogContract
    {
        public long Id { get; init; }

        public long ExecutionId { get; init; }

        public LogLevelType Level { get; init; }

        public string Message { get; init; } = string.Empty;

        public string? Context { get; init; }

        public string? Request { get; init; }

        public string? Response { get; init; }

        public int? HttpStatusCode { get; init; }

        public long? Duration { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public ExecutionLogPipelineStepContract? PipelineStep { get; init; }

        public static Expression<Func<ExecutionLog, ExecutionLogContract>> Projection => item => new ExecutionLogContract
        {
            Id = item.Id,
            ExecutionId = item.ExecutionId,
            Level = item.Level,
            Message = item.Message,
            Context = item.Context,
            Request = item.Request,
            Response = item.Response,
            HttpStatusCode = item.HttpStatusCode,
            Duration = item.Duration,
            CreatedAt = item.CreatedAt,
            PipelineStep = item.PipelineStep == null
                ? null
                : new ExecutionLogPipelineStepContract
                {
                    Id = item.PipelineStep.Id,
                    Order = item.PipelineStep.Order,
                    Name = item.PipelineStep.Name,
                    Type = item.PipelineStep.Type,
                    ErrorAction = item.PipelineStep.ErrorAction,
                    IsActive = item.PipelineStep.IsActive,
                    IgnoreOnResponse = item.PipelineStep.IgnoreOnResponse
                }
        };
    }

    public sealed class ExecutionLogPipelineStepContract
    {
        public long Id { get; init; }

        public int Order { get; init; }

        public string Name { get; init; } = string.Empty;

        public PipelineStepType Type { get; init; }

        public ErrorAction ErrorAction { get; init; }

        public bool IsActive { get; init; }

        public bool IgnoreOnResponse { get; init; }
    }
}
