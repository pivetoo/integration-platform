using System.ComponentModel.DataAnnotations;

namespace IntegrationPlatform.Application.Requests.ConnectorAttributeValues
{
    public sealed class CreateConnectorAttributeValueRequest
    {
        [Range(1, long.MaxValue)]
        public long ConnectorId { get; set; }

        [Range(1, long.MaxValue)]
        public long IntegrationAttributeId { get; set; }

        [Required]
        public string Value { get; set; } = string.Empty;
    }
}
