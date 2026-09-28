using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace IntegrationPlatform.Api.Contracts.ProcessingQueues
{
    public sealed class EnqueuePipelineRequest
    {
        [Range(1, long.MaxValue)]
        public long ConnectorId { get; set; }

        [Range(1, long.MaxValue)]
        public long PipelineId { get; set; }

        public JsonElement? Payload { get; set; }

        public int Priority { get; set; }

        [StringLength(200)]
        public string? IdempotencyKey { get; set; }
    }
}
