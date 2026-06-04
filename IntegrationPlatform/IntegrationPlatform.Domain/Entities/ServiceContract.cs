using Archon.Core.Entities;

namespace IntegrationPlatform.Domain.Entities
{
    public class ServiceContract : Entity
    {
        private readonly List<IntegrationServiceContract> integrationServiceContracts = [];
        private readonly List<Pipeline> pipelines = [];

        public string Identifier { get; private set; } = string.Empty;

        public string Name { get; private set; } = string.Empty;

        public string? Description { get; private set; }

        public long IntegrationCategoryId { get; private set; }

        public IntegrationCategory IntegrationCategory { get; private set; } = null!;

        public string? InputSchema { get; private set; }

        public string? OutputSchema { get; private set; }

        public bool HasCallback { get; private set; }

        public string? CallbackSchema { get; private set; }

        // Por padrao (false) o callback leva apenas identificadores + status; o output da execucao
        // so e incluido quando explicitamente habilitado (opt-in), evitando vazar dados sensiveis.
        public bool IncludeOutputInCallback { get; private set; }

        public bool IsActive { get; private set; } = true;

        public bool IsSystem { get; private set; }

        public IReadOnlyCollection<IntegrationServiceContract> IntegrationServiceContracts => integrationServiceContracts.AsReadOnly();

        public IReadOnlyCollection<Pipeline> Pipelines => pipelines.AsReadOnly();

        private ServiceContract()
        {
        }

        public ServiceContract(string identifier, string name, long integrationCategoryId, string? description = null, string? inputSchema = null, string? outputSchema = null, bool hasCallback = false, string? callbackSchema = null, bool isSystem = false)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            if (integrationCategoryId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(integrationCategoryId));
            }

            Identifier = NormalizeIdentifier(identifier);
            Name = name.Trim();
            Description = description?.Trim();
            IntegrationCategoryId = integrationCategoryId;
            InputSchema = inputSchema?.Trim();
            OutputSchema = outputSchema?.Trim();
            HasCallback = hasCallback;
            CallbackSchema = callbackSchema?.Trim();
            IsSystem = isSystem;
        }

        public void Update(string identifier, string name, long integrationCategoryId, string? description, string? inputSchema, string? outputSchema, bool hasCallback, string? callbackSchema, bool isActive)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            if (integrationCategoryId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(integrationCategoryId));
            }

            string normalizedIdentifier = NormalizeIdentifier(identifier);
            if (IsSystem && !string.Equals(normalizedIdentifier, Identifier, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("serviceContract.identifier.systemReadonly");
            }

            Identifier = normalizedIdentifier;
            Name = name.Trim();
            Description = description?.Trim();
            IntegrationCategoryId = integrationCategoryId;
            InputSchema = inputSchema?.Trim();
            OutputSchema = outputSchema?.Trim();
            HasCallback = hasCallback;
            CallbackSchema = callbackSchema?.Trim();
            IsActive = isActive;
        }

        public void SetCallbackOutputInclusion(bool include)
        {
            IncludeOutputInCallback = include;
        }

        private static string NormalizeIdentifier(string value)
        {
            return value.Trim().ToLowerInvariant();
        }
    }
}
