using Archon.Core.Entities;
using IntegrationPlatform.Domain.ValueObjects;

namespace IntegrationPlatform.Domain.Entities
{
    public class IntegrationAttribute : Entity
    {
        private readonly List<ConnectorAttributeValue> connectorAttributeValues = [];

        public long IntegrationId { get; private set; }

        public Integration Integration { get; private set; } = null!;

        public string Field { get; private set; } = string.Empty;

        public string Label { get; private set; } = string.Empty;

        public string? Description { get; private set; }

        public string? Placeholder { get; private set; }

        public FieldType Type { get; private set; }

        public string? DefaultValue { get; private set; }

        public bool IsRequired { get; private set; }

        public int Order { get; private set; }

        public string? Group { get; private set; }

        public bool IsSensitive { get; private set; }

        public bool IsHidden { get; private set; }

        public IReadOnlyCollection<ConnectorAttributeValue> ConnectorAttributeValues => connectorAttributeValues.AsReadOnly();

        private IntegrationAttribute()
        {
        }

        public IntegrationAttribute(long integrationId, string field, string label, FieldType type, bool isRequired, int order, string? description = null, string? placeholder = null, string? defaultValue = null, string? group = null, bool isSensitive = false, bool isHidden = false)
        {
            if (integrationId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(integrationId));
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(field);
            ArgumentException.ThrowIfNullOrWhiteSpace(label);

            IntegrationId = integrationId;
            Field = field.Trim();
            Label = label.Trim();
            Description = description?.Trim();
            Placeholder = placeholder?.Trim();
            Type = type;
            DefaultValue = defaultValue;
            IsRequired = isRequired;
            Order = order;
            Group = group?.Trim();
            IsSensitive = isSensitive;
            IsHidden = isHidden;
        }

        public void Update(string field, string label, FieldType type, bool isRequired, int order, string? description, string? placeholder, string? defaultValue, string? group, bool isSensitive, bool isHidden)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(field);
            ArgumentException.ThrowIfNullOrWhiteSpace(label);

            Field = field.Trim();
            Label = label.Trim();
            Description = description?.Trim();
            Placeholder = placeholder?.Trim();
            Type = type;
            DefaultValue = defaultValue;
            IsRequired = isRequired;
            Order = order;
            Group = group?.Trim();
            IsSensitive = isSensitive;
            IsHidden = isHidden;
        }
    }
}
