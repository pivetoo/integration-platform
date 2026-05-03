using IntegrationPlatform.Api.Contracts.Connectors;
using IntegrationPlatform.Domain.Entities;
using System.Linq.Expressions;

namespace IntegrationPlatform.Api.Contracts.References
{
    public sealed class ReferenceContract
    {
        public long Id { get; init; }

        public long ConnectorId { get; init; }

        public string Entity { get; init; } = string.Empty;

        public string InternalId { get; init; } = string.Empty;

        public string ExternalId { get; init; } = string.Empty;

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }

        public ConnectorContract? Connector { get; init; }

        public static Expression<Func<Reference, ReferenceContract>> Projection => item => new ReferenceContract
        {
            Id = item.Id,
            ConnectorId = item.ConnectorId,
            Entity = item.EntityName,
            InternalId = item.InternalId,
            ExternalId = item.ExternalId,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt,
            Connector = item.Connector == null
                ? null
                : new ConnectorContract
                {
                    Id = item.Connector.Id,
                    SystemApplicationId = item.Connector.SystemApplicationId,
                    IntegrationId = item.Connector.IntegrationId,
                    Name = item.Connector.Name,
                    IsActive = item.Connector.IsActive,
                    CreatedAt = item.Connector.CreatedAt,
                    UpdatedAt = item.Connector.UpdatedAt,
                    Integration = item.Connector.Integration == null
                        ? null
                        : new Contracts.Integrations.IntegrationContract
                        {
                            Id = item.Connector.Integration.Id,
                            Identifier = item.Connector.Integration.Identifier,
                            Name = item.Connector.Integration.Name,
                            Description = item.Connector.Integration.Description,
                            IntegrationCategoryId = item.Connector.Integration.IntegrationCategoryId,
                            IsActive = item.Connector.Integration.IsActive,
                            CreatedAt = item.Connector.Integration.CreatedAt,
                            UpdatedAt = item.Connector.Integration.UpdatedAt,
                            IntegrationCategory = item.Connector.Integration.IntegrationCategory == null
                                ? null
                                : new Contracts.Integrations.IntegrationCategoryContract
                                {
                                    Id = item.Connector.Integration.IntegrationCategory.Id,
                                    Name = item.Connector.Integration.IntegrationCategory.Name,
                                    Description = item.Connector.Integration.IntegrationCategory.Description,
                                    IsActive = item.Connector.Integration.IntegrationCategory.IsActive,
                                    CreatedAt = item.Connector.Integration.IntegrationCategory.CreatedAt,
                                    UpdatedAt = item.Connector.Integration.IntegrationCategory.UpdatedAt
                                }
                        }
                }
        };
    }
}
