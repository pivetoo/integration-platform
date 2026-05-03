using Archon.Core.Entities;

namespace IntegrationPlatform.Domain.Entities
{
    public class Reference : Entity
    {
        public long ConnectorId { get; private set; }

        public Connector Connector { get; private set; } = null!;

        public string EntityName { get; private set; } = string.Empty;

        public string InternalId { get; private set; } = string.Empty;

        public string ExternalId { get; private set; } = string.Empty;

        private Reference()
        {
        }

        public Reference(long connectorId, string entityName, string internalId, string externalId)
        {
            if (connectorId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(connectorId));
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(entityName);
            ArgumentException.ThrowIfNullOrWhiteSpace(internalId);
            ArgumentException.ThrowIfNullOrWhiteSpace(externalId);

            ConnectorId = connectorId;
            EntityName = entityName.Trim();
            InternalId = internalId.Trim();
            ExternalId = externalId.Trim();
        }

        public void Update(string entityName, string internalId, string externalId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(entityName);
            ArgumentException.ThrowIfNullOrWhiteSpace(internalId);
            ArgumentException.ThrowIfNullOrWhiteSpace(externalId);

            EntityName = entityName.Trim();
            InternalId = internalId.Trim();
            ExternalId = externalId.Trim();
        }
    }
}
