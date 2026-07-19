using Archon.Core.Entities;
using IntegrationPlatform.Domain.ValueObjects;

namespace IntegrationPlatform.Domain.Entities
{
    public class PipelineStep : Entity
    {
        private readonly List<ExecutionLog> executionLogs = [];

        public long PipelineId { get; private set; }

        public Pipeline Pipeline { get; private set; } = null!;

        public int Order { get; private set; }

        public string Name { get; private set; } = string.Empty;

        public PipelineStepType Type { get; private set; }

        public long? ApiCallId { get; private set; }

        public ApiCall? ApiCall { get; private set; }

        public long? JavaScriptFunctionId { get; private set; }

        public JavaScriptFunction? JavaScriptFunction { get; private set; }

        public long? DatabaseScriptId { get; private set; }

        public DatabaseScript? DatabaseScript { get; private set; }

        public ErrorAction ErrorAction { get; private set; }

        public bool IsActive { get; private set; } = true;

        public bool IgnoreOnResponse { get; private set; }

        public bool RunOnError { get; private set; }

        public string? RunCondition { get; private set; }

        public IReadOnlyCollection<ExecutionLog> ExecutionLogs => executionLogs.AsReadOnly();

        private PipelineStep()
        {
        }

        public PipelineStep(long pipelineId, int order, string name, PipelineStepType type, ErrorAction errorAction, long? apiCallId = null, long? javaScriptFunctionId = null, long? databaseScriptId = null, bool ignoreOnResponse = false, bool runOnError = false, string? runCondition = null)
        {
            if (pipelineId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pipelineId));
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            PipelineId = pipelineId;
            Order = order;
            Name = name.Trim();
            Type = type;
            ApiCallId = apiCallId;
            JavaScriptFunctionId = javaScriptFunctionId;
            DatabaseScriptId = databaseScriptId;
            ErrorAction = errorAction;
            IgnoreOnResponse = ignoreOnResponse;
            RunOnError = runOnError;
            RunCondition = NormalizeRunCondition(runCondition);
        }

        public void Update(int order, string name, PipelineStepType type, ErrorAction errorAction, long? apiCallId, long? javaScriptFunctionId, long? databaseScriptId, bool isActive, bool ignoreOnResponse, bool runOnError = false, string? runCondition = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            Order = order;
            Name = name.Trim();
            Type = type;
            ApiCallId = apiCallId;
            JavaScriptFunctionId = javaScriptFunctionId;
            DatabaseScriptId = databaseScriptId;
            ErrorAction = errorAction;
            IsActive = isActive;
            IgnoreOnResponse = ignoreOnResponse;
            RunOnError = runOnError;
            RunCondition = NormalizeRunCondition(runCondition);
        }

        private static string? NormalizeRunCondition(string? runCondition)
        {
            return string.IsNullOrWhiteSpace(runCondition) ? null : runCondition.Trim();
        }
    }
}
