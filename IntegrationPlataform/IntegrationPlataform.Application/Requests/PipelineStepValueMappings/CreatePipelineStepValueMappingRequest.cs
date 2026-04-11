using System.ComponentModel.DataAnnotations;

namespace IntegrationPlataform.Application.Requests.PipelineStepValueMappings
{
    public sealed class CreatePipelineStepValueMappingRequest
    {
        [Range(1, long.MaxValue)]
        public long PipelineStepId { get; set; }

        public long? SourcePipelineStepId { get; set; }

        public long? PipelineExampleId { get; set; }

        [Required]
        [StringLength(300, MinimumLength = 1)]
        public string TargetField { get; set; } = string.Empty;

        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string SourceType { get; set; } = string.Empty;

        [Required]
        [StringLength(500, MinimumLength = 1)]
        public string SourcePath { get; set; } = string.Empty;

        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string ValueType { get; set; } = string.Empty;

        public string? FixedValue { get; set; }

        public int Order { get; set; }
    }
}
