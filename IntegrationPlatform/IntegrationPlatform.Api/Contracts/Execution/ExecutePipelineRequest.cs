using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace IntegrationPlatform.Api.Contracts.Execution
{
    public sealed class ExecutePipelineRequest
    {
        [Range(1, long.MaxValue)]
        public long ConnectorId { get; set; }

        [Range(1, long.MaxValue)]
        public long PipelineId { get; set; }

        public JsonElement? InputData { get; set; }
    }
}
