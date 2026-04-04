using System.ComponentModel.DataAnnotations;

namespace IntegrationPlataform.Application.Requests.IntegrationCategories
{
    public sealed class CreateIntegrationCategoryRequest
    {
        [Required]
        [StringLength(200, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }
    }
}
