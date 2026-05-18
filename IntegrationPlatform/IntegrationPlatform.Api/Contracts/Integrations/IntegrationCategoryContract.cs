namespace IntegrationPlatform.Api.Contracts.Integrations
{
    public sealed class IntegrationCategoryContract
    {
        public long Id { get; init; }

        public string Identifier { get; init; } = string.Empty;

        public string Name { get; init; } = string.Empty;

        public string? Description { get; init; }

        public bool IsActive { get; init; }

        public bool IsSystem { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }
    }
}
