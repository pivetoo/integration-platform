using System.ComponentModel.DataAnnotations;

namespace IntegrationPlatform.Api.Contracts.Execution
{
    public sealed class DebugPipelineRequest
    {
        [Range(1, long.MaxValue)]
        public long ConnectorId { get; set; }

        [Range(1, long.MaxValue)]
        public long PipelineId { get; set; }

        public string? InputData { get; set; }

        public long? InitialStepId { get; set; }
    }
}
