using System.ComponentModel.DataAnnotations;

namespace IntegrationPlatform.Application.Requests.PipelineRoutines
{
    public sealed class UpdatePipelineRoutineRequest
    {
        public bool IsActive { get; set; }

        [Range(1, int.MaxValue)]
        public int IntervalMinutes { get; set; }

        public string? DefaultPayload { get; set; }

        public DateTimeOffset? NextExecution { get; set; }
    }
}
