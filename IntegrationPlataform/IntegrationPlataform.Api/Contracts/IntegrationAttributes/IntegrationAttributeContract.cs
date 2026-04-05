using IntegrationPlataform.Domain.Entities;
using IntegrationPlataform.Domain.ValueObjects;
using System.Linq.Expressions;

namespace IntegrationPlataform.Api.Contracts.IntegrationAttributes
{
    public sealed class IntegrationAttributeContract
    {
        public long Id { get; init; }

        public long IntegrationId { get; init; }

        public string Field { get; init; } = string.Empty;

        public string Label { get; init; } = string.Empty;

        public string? Description { get; init; }

        public string? Placeholder { get; init; }

        public FieldType Type { get; init; }

        public string? DefaultValue { get; init; }

        public bool IsRequired { get; init; }

        public int Order { get; init; }

        public string? Group { get; init; }

        public bool IsSensitive { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }

        public static Expression<Func<IntegrationAttribute, IntegrationAttributeContract>> Projection => item => new IntegrationAttributeContract
        {
            Id = item.Id,
            IntegrationId = item.IntegrationId,
            Field = item.Field,
            Label = item.Label,
            Description = item.Description,
            Placeholder = item.Placeholder,
            Type = item.Type,
            DefaultValue = item.DefaultValue,
            IsRequired = item.IsRequired,
            Order = item.Order,
            Group = item.Group,
            IsSensitive = item.IsSensitive,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }
}
