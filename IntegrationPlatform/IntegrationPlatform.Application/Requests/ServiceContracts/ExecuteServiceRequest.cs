using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace IntegrationPlatform.Application.Requests.ServiceContracts
{
    public sealed class ExecuteServiceRequest
    {
        [Required]
        public long ConnectorId { get; set; }

        public JsonElement? InputData { get; set; }
    }

    public sealed class EnqueueServiceRequest
    {
        [Required]
        public long ConnectorId { get; set; }

        public JsonElement? InputData { get; set; }

        public int Priority { get; set; } = 5;

        public DateTime? ScheduledFor { get; set; }
    }
}
