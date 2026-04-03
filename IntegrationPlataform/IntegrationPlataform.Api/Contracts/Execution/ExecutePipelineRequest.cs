using System.ComponentModel.DataAnnotations;

namespace IntegrationPlataform.Api.Contracts.Execution
{
    public sealed class ExecutePipelineRequest
    {
        [Range(1, long.MaxValue)]
        public long ConnectorId { get; set; }

        [Range(1, long.MaxValue)]
        public long PipelineId { get; set; }

        public string? InputData { get; set; }
    }
}
