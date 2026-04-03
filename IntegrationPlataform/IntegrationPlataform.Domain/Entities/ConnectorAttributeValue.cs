using Archon.Core.Entities;

namespace IntegrationPlataform.Domain.Entities
{
    public class ConnectorAttributeValue : Entity
    {
        public long ConnectorId { get; private set; }

        public Connector Connector { get; private set; } = null!;

        public long IntegrationAttributeId { get; private set; }

        public IntegrationAttribute IntegrationAttribute { get; private set; } = null!;

        public string Value { get; private set; } = string.Empty;

        private ConnectorAttributeValue()
        {
        }

        public ConnectorAttributeValue(long connectorId, long integrationAttributeId, string value)
        {
            if (connectorId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(connectorId));
            }

            if (integrationAttributeId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(integrationAttributeId));
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(value);

            ConnectorId = connectorId;
            IntegrationAttributeId = integrationAttributeId;
            Value = value;
        }

        public void Update(long integrationAttributeId, string value)
        {
            if (integrationAttributeId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(integrationAttributeId));
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(value);

            IntegrationAttributeId = integrationAttributeId;
            Value = value;
        }
    }
}
