using System.ComponentModel.DataAnnotations;

namespace IntegrationPlatform.Application.Requests.Connectors
{
    public sealed class UpdateConnectorRequest
    {
        [Required]
        public long Id { get; set; }

        [Range(1, long.MaxValue)]
        public long IntegrationId { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? SystemApplicationId { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(500)]
        public string? CallbackUrl { get; set; }

        [StringLength(120)]
        public string? CallbackToken { get; set; }
    }
}
