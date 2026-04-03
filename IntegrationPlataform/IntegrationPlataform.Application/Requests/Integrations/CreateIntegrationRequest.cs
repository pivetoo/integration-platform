using System.ComponentModel.DataAnnotations;

namespace IntegrationPlataform.Application.Requests.Integrations
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

        public long? IntegrationCategoryId { get; set; }
    }
}
