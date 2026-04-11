using System.ComponentModel.DataAnnotations;

namespace IntegrationPlataform.Application.Requests.PipelineStepExamples
{
    public sealed class UpdatePipelineStepExampleRequest
    {
        [Required]
        public long Id { get; set; }

        [Range(1, long.MaxValue)]
        public long PipelineStepId { get; set; }

        public long? PipelineExampleId { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        public string? RequestExample { get; set; }

        public string? ResponseExample { get; set; }

        public bool IsDefault { get; set; }
    }
}
