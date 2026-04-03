using Archon.Core.Entities;

namespace IntegrationPlataform.Domain.Entities
{
    public class IntegrationCategory : Entity
    {
        private readonly List<Integration> integrations = [];

        public string Name { get; private set; } = string.Empty;

        public string? Description { get; private set; }

        public bool IsActive { get; private set; } = true;

        public IReadOnlyCollection<Integration> Integrations => integrations.AsReadOnly();

        private IntegrationCategory()
        {
        }

        public IntegrationCategory(string name, string? description = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            Name = name.Trim();
            Description = description?.Trim();
        }

        public void Update(string name, string? description, bool isActive)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            Name = name.Trim();
            Description = description?.Trim();
            IsActive = isActive;
        }
    }
}
