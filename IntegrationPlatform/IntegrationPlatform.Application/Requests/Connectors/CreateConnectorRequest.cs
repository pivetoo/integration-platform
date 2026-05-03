using System.ComponentModel.DataAnnotations;

namespace IntegrationPlatform.Application.Requests.Connectors
{
    public sealed class CreateConnectorRequest
    {
        [Range(1, long.MaxValue)]
        public long IntegrationId { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? SystemApplicationId { get; set; }
    }
}
