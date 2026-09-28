using System.ComponentModel.DataAnnotations;

namespace IntegrationPlatform.Application.Requests.Pipelines
{
    public sealed class UpdatePipelineRequest
    {
        [Required]
        public long Id { get; set; }

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

        public bool IsActive { get; set; } = true;

        [Range(1, 10)]
        public int MaxAttempts { get; set; } = 1;
    }
}
