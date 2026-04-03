using System.ComponentModel.DataAnnotations;

namespace IntegrationPlataform.Api.Contracts.ProcessingQueues
{
    public sealed class EnqueuePipelineRequest
    {
        [Range(1, long.MaxValue)]
        public long ConnectorId { get; set; }

        [Range(1, long.MaxValue)]
        public long PipelineId { get; set; }

        public string? Payload { get; set; }

        public int Priority { get; set; }
    }
}
