using IntegrationPlataform.Api.Contracts.Integrations;
using IntegrationPlataform.Domain.Entities;
using System.Linq.Expressions;

namespace IntegrationPlataform.Api.Contracts.Connectors
{
    public sealed class ConnectorContract
    {
        public long Id { get; init; }

        public string? SystemApplicationId { get; init; }

        public long IntegrationId { get; init; }

        public string Name { get; init; } = string.Empty;

        public bool IsActive { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }

        public IntegrationContract? Integration { get; init; }

        public static Expression<Func<Connector, ConnectorContract>> Projection => item => new ConnectorContract
        {
            Id = item.Id,
            SystemApplicationId = item.SystemApplicationId,
            IntegrationId = item.IntegrationId,
            Name = item.Name,
            IsActive = item.IsActive,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt,
            Integration = item.Integration == null
                ? null
                : new IntegrationContract
                {
                    Id = item.Integration.Id,
                    Identifier = item.Integration.Identifier,
                    Name = item.Integration.Name,
                    Description = item.Integration.Description,
                    IntegrationCategoryId = item.Integration.IntegrationCategoryId,
                    IsActive = item.Integration.IsActive,
                    CreatedAt = item.Integration.CreatedAt,
                    UpdatedAt = item.Integration.UpdatedAt,
                    IntegrationCategory = item.Integration.IntegrationCategory == null
                        ? null
                        : new IntegrationCategoryContract
                        {
                            Id = item.Integration.IntegrationCategory.Id,
                            Name = item.Integration.IntegrationCategory.Name,
                            Description = item.Integration.IntegrationCategory.Description,
                            IsActive = item.Integration.IntegrationCategory.IsActive,
                            CreatedAt = item.Integration.IntegrationCategory.CreatedAt,
                            UpdatedAt = item.Integration.IntegrationCategory.UpdatedAt
                        }
                }
        };
    }
}
