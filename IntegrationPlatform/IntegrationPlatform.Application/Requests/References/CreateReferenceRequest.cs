using System.ComponentModel.DataAnnotations;

namespace IntegrationPlatform.Application.Requests.References
{
    public sealed class CreateReferenceRequest
    {
        [Range(1, long.MaxValue)]
        public long ConnectorId { get; set; }

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
