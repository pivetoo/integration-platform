using IntegrationPlataform.Domain.Entities;
using System.Linq.Expressions;

namespace IntegrationPlataform.Api.Contracts.Integrations
{
    public sealed class IntegrationContract
    {
        public long Id { get; init; }

        public string Identifier { get; init; } = string.Empty;

        public string Name { get; init; } = string.Empty;

        public string? Description { get; init; }

        public long? IntegrationCategoryId { get; init; }

        public bool IsActive { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }

        public IntegrationCategoryContract? IntegrationCategory { get; init; }

        public static Expression<Func<Integration, IntegrationContract>> Projection => item => new IntegrationContract
        {
            Id = item.Id,
            Identifier = item.Identifier,
            Name = item.Name,
            Description = item.Description,
            IntegrationCategoryId = item.IntegrationCategoryId,
            IsActive = item.IsActive,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt,
            IntegrationCategory = item.IntegrationCategory == null
                ? null
                : new IntegrationCategoryContract
                {
                    Id = item.IntegrationCategory.Id,
                    Name = item.IntegrationCategory.Name,
                    Description = item.IntegrationCategory.Description,
                    IsActive = item.IntegrationCategory.IsActive,
                    CreatedAt = item.IntegrationCategory.CreatedAt,
                    UpdatedAt = item.IntegrationCategory.UpdatedAt
                }
        };
    }
}
