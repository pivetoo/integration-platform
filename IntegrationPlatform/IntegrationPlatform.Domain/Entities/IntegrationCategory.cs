using Archon.Core.Entities;

namespace IntegrationPlatform.Domain.Entities
{
    public class IntegrationCategory : Entity
    {
        private readonly List<Integration> integrations = [];

        public string Identifier { get; private set; } = string.Empty;

        public string Name { get; private set; } = string.Empty;

        public string? Description { get; private set; }

        public bool IsActive { get; private set; } = true;

        public IReadOnlyCollection<Integration> Integrations => integrations.AsReadOnly();

        private IntegrationCategory()
        {
        }

        public IntegrationCategory(string identifier, string name, string? description = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            Identifier = NormalizeIdentifier(identifier);
            Name = name.Trim();
            Description = description?.Trim();
        }

        public void Update(string identifier, string name, string? description, bool isActive)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            string normalizedIdentifier = NormalizeIdentifier(identifier);

            Identifier = normalizedIdentifier;
            Name = name.Trim();
            Description = description?.Trim();
            IsActive = isActive;
        }

        private static string NormalizeIdentifier(string value)
        {
            return value.Trim().ToLowerInvariant();
        }
    }
}
