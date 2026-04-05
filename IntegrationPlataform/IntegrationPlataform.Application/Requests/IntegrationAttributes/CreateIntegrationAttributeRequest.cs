using IntegrationPlataform.Domain.ValueObjects;
using System.ComponentModel.DataAnnotations;

namespace IntegrationPlataform.Application.Requests.IntegrationAttributes
{
    public sealed class CreateIntegrationAttributeRequest
    {
        [Range(1, long.MaxValue)]
        public long IntegrationId { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 2)]
        public string Field { get; set; } = string.Empty;

        [Required]
        [StringLength(200, MinimumLength = 2)]
        public string Label { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? Placeholder { get; set; }

        [Required]
        public FieldType Type { get; set; }

        public string? DefaultValue { get; set; }

        public bool IsRequired { get; set; }

        public int Order { get; set; }

        public string? Group { get; set; }

        public bool IsSensitive { get; set; }
    }
}
