using System.ComponentModel.DataAnnotations;

namespace IntegrationPlatform.Application.Requests.Integrations
{
    public sealed class CreateIntegrationRequest
    {
        [Required]
        [StringLength(50, MinimumLength = 2)]
        public string Identifier { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(500)]
        public string? IconUrl { get; set; }

        public long? IntegrationCategoryId { get; set; }
    }
}
