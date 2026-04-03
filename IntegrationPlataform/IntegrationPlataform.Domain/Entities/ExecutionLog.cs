using Archon.Core.Entities;
using IntegrationPlataform.Domain.ValueObjects;

namespace IntegrationPlataform.Domain.Entities
{
    public class ExecutionLog : Entity
    {
        public long ExecutionId { get; private set; }

        public Execution Execution { get; private set; } = null!;

        public long? PipelineStepId { get; private set; }

        public PipelineStep? PipelineStep { get; private set; }

        public LogLevelType Level { get; private set; }

        public string Message { get; private set; } = string.Empty;

        public string? Context { get; private set; }

        public string? Request { get; private set; }

        public string? Response { get; private set; }

        public int? HttpStatusCode { get; private set; }

        public long? Duration { get; private set; }

        private ExecutionLog()
        {
        }

        public ExecutionLog(long executionId, LogLevelType level, string message, long? pipelineStepId = null, string? context = null, string? request = null, string? response = null, int? httpStatusCode = null, long? duration = null)
        {
            if (executionId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(executionId));
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(message);

            ExecutionId = executionId;
            PipelineStepId = pipelineStepId;
            Level = level;
            Message = message.Trim();
            Context = context;
            Request = request;
            Response = response;
            HttpStatusCode = httpStatusCode;
            Duration = duration;
        }
    }
}
