using Archon.Core.Entities;

namespace IntegrationPlatform.Domain.Entities
{
    public class Integration : Entity
    {
        private readonly List<IntegrationAttribute> attributes = [];
        private readonly List<Pipeline> pipelines = [];
        private readonly List<Connector> connectors = [];

        public string Identifier { get; private set; } = string.Empty;

        public string Name { get; private set; } = string.Empty;

        public string? Description { get; private set; }

        public string? IconUrl { get; private set; }

        public long? IntegrationCategoryId { get; private set; }

        public IntegrationCategory? IntegrationCategory { get; private set; }

        public bool IsActive { get; private set; } = true;

        public bool SupportsWebhook { get; private set; }

        public IReadOnlyCollection<IntegrationAttribute> Attributes => attributes.AsReadOnly();

        public IReadOnlyCollection<Pipeline> Pipelines => pipelines.AsReadOnly();

        public IReadOnlyCollection<Connector> Connectors => connectors.AsReadOnly();

        private Integration()
        {
        }

        public Integration(string identifier, string name, string? description = null, long? integrationCategoryId = null, string? iconUrl = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            Identifier = identifier.Trim();
            Name = name.Trim();
            Description = description?.Trim();
            IntegrationCategoryId = integrationCategoryId;
            IconUrl = iconUrl?.Trim();
        }

        public void Update(string identifier, string name, string? description, long? integrationCategoryId, bool isActive, string? iconUrl, bool supportsWebhook = false)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            Identifier = identifier.Trim();
            Name = name.Trim();
            Description = description?.Trim();
            IntegrationCategoryId = integrationCategoryId;
            IsActive = isActive;
            IconUrl = iconUrl?.Trim();
            SupportsWebhook = supportsWebhook;
        }
    }
}
