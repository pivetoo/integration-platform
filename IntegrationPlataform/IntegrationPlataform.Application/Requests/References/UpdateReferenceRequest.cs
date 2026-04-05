using System.ComponentModel.DataAnnotations;

namespace IntegrationPlataform.Application.Requests.References
{
    public sealed class UpdateReferenceRequest
    {
        [Required]
        [StringLength(200, MinimumLength = 2)]
        public string Entity { get; set; } = string.Empty;

        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string InternalId { get; set; } = string.Empty;

        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string ExternalId { get; set; } = string.Empty;
    }
}
