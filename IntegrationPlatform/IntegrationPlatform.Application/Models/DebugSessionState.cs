using IntegrationPlatform.Domain.Entities;
using System.Diagnostics;

namespace IntegrationPlatform.Application.Models
{
    public class DebugSessionState
    {
        public string SessionId { get; set; } = string.Empty;

        public Execution Execution { get; set; } = null!;

        public Connector Connector { get; set; } = null!;

        public Pipeline Pipeline { get; set; } = null!;

        public PipelineExecutionContext Context { get; set; } = null!;

        public List<PipelineStep> ActiveSteps { get; set; } = [];

        public Dictionary<string, object?> StepOutputs { get; set; } = [];

        public int NextOutputIndex { get; set; } = 1;

        public int CurrentIndex { get; set; }

        public bool HasFailure { get; set; }

        public bool Stopped { get; set; }

        public Stopwatch Stopwatch { get; set; } = null!;
    }
}
