using System.ComponentModel.DataAnnotations;

namespace IntegrationPlatform.Application.Requests.ServiceContracts
{
    public sealed class UpdateServiceContractRequest
    {
        [Required]
        public long Id { get; set; }

        [Required]
        [StringLength(120, MinimumLength = 2)]
        [RegularExpression("^[a-z0-9]+(?:[.-][a-z0-9]+)*$", ErrorMessage = "validation.serviceContract.identifier.invalidFormat")]
        public string Identifier { get; set; } = string.Empty;

        [Required]
        [StringLength(200, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public long IntegrationCategoryId { get; set; }

        public string? InputSchema { get; set; }

        public string? OutputSchema { get; set; }

        public bool HasCallback { get; set; }

        public string? CallbackSchema { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
