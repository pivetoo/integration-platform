using System.ComponentModel.DataAnnotations;

namespace IntegrationPlataform.Application.Requests.ConnectorAttributeValues
{
    public sealed class UpdateConnectorAttributeValueRequest
    {
        [Required]
        public long Id { get; set; }

        [Range(1, long.MaxValue)]
        public long IntegrationAttributeId { get; set; }

        [Required]
        public string Value { get; set; } = string.Empty;
    }
}
