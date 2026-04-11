using System.ComponentModel.DataAnnotations;

namespace IntegrationPlataform.Application.Requests.PipelineExamples
{
    public sealed class CreatePipelineExampleRequest
    {
        [Range(1, long.MaxValue)]
        public long PipelineId { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public string? InputPayloadExample { get; set; }

        public string? ExpectedOutputExample { get; set; }

        public bool IsDefault { get; set; }
    }
}
