using System.ComponentModel.DataAnnotations;

namespace IntegrationPlatform.Application.Requests.IntegrationCategories
{
    public sealed class CreateIntegrationCategoryRequest
    {
        [Required]
        [StringLength(80, MinimumLength = 2)]
        [RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$", ErrorMessage = "validation.integrationCategory.identifier.invalidFormat")]
        public string Identifier { get; set; } = string.Empty;

        [Required]
        [StringLength(200, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }
    }
}
