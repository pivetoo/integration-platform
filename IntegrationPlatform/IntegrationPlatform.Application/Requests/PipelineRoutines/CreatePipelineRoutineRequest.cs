using System.ComponentModel.DataAnnotations;

namespace IntegrationPlatform.Application.Requests.PipelineRoutines
{
    public sealed class CreatePipelineRoutineRequest
    {
        [Range(1, long.MaxValue)]
        public long ConnectorId { get; set; }

        [Range(1, long.MaxValue)]
        public long PipelineId { get; set; }

        public bool IsActive { get; set; } = true;

        [Range(1, int.MaxValue)]
        public int IntervalMinutes { get; set; }

        public string? DefaultPayload { get; set; }

        public DateTimeOffset? NextExecution { get; set; }
    }
}
