using IntegrationPlatform.Api.Contracts.Integrations;
using IntegrationPlatform.Domain.Entities;
using System.Linq.Expressions;

namespace IntegrationPlatform.Api.Contracts.Pipelines
{
    public sealed class PipelineContract
    {
        public long Id { get; init; }

        public long IntegrationId { get; init; }

        public string Identifier { get; init; } = string.Empty;

        public string Name { get; init; } = string.Empty;

        public string? Description { get; init; }

        public bool IsActive { get; init; }

        public bool IsDefault { get; init; }

        public bool IsTestPipeline { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }

        public IntegrationContract? Integration { get; init; }

        public static Expression<Func<Pipeline, PipelineContract>> Projection => item => new PipelineContract
        {
            Id = item.Id,
            IntegrationId = item.IntegrationId,
            Identifier = item.Identifier,
            Name = item.Name,
            Description = item.Description,
            IsActive = item.IsActive,
            IsDefault = item.IsDefault,
            IsTestPipeline = item.IsTestPipeline,
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
