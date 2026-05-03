using IntegrationPlatform.Domain.ValueObjects;
using System.ComponentModel.DataAnnotations;

namespace IntegrationPlatform.Application.Requests.PipelineSteps
{
    public sealed class UpdatePipelineStepRequest
    {
        [Required]
        public long Id { get; set; }

        public int Order { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public PipelineStepType Type { get; set; }

        [Required]
        public ErrorAction ErrorAction { get; set; }

        public long? ApiCallId { get; set; }

        public long? JavaScriptFunctionId { get; set; }

        public long? DatabaseScriptId { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IgnoreOnResponse { get; set; }
    }
}
