using System.ComponentModel.DataAnnotations;

namespace IntegrationPlataform.Application.Requests.Pipelines
{
    public sealed class CreatePipelineRequest
    {
        [Range(1, long.MaxValue)]
        public long IntegrationId { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string Identifier { get; set; } = string.Empty;

        [Required]
        [StringLength(200, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }
    }
}
