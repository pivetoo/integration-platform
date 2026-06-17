using IntegrationPlatform.Api.Contracts.Pipelines;
using IntegrationPlatform.Domain.Entities;
using IntegrationPlatform.Domain.ValueObjects;
using System.Linq.Expressions;

namespace IntegrationPlatform.Api.Contracts.PipelineSteps
{
    public sealed class PipelineStepContract
    {
        public long Id { get; init; }

        public long PipelineId { get; init; }

        public int Order { get; init; }

        public string Name { get; init; } = string.Empty;

        public PipelineStepType Type { get; init; }

        public ErrorAction ErrorAction { get; init; }

        public bool IsActive { get; init; }

        public bool IgnoreOnResponse { get; init; }

        public bool RunOnError { get; init; }

        public PipelineContract? Pipeline { get; init; }

        public PipelineStepReferenceContract? ApiCall { get; init; }

        public PipelineStepReferenceContract? JavaScriptFunction { get; init; }

        public PipelineStepReferenceContract? DatabaseScript { get; init; }

        public static Expression<Func<PipelineStep, PipelineStepContract>> Projection => item => new PipelineStepContract
        {
            Id = item.Id,
            PipelineId = item.PipelineId,
            Order = item.Order,
            Name = item.Name,
            Type = item.Type,
            ErrorAction = item.ErrorAction,
            IsActive = item.IsActive,
            IgnoreOnResponse = item.IgnoreOnResponse,
            RunOnError = item.RunOnError,
            Pipeline = item.Pipeline == null
                ? null
                : new PipelineContract
                {
                    Id = item.Pipeline.Id,
                    IntegrationId = item.Pipeline.IntegrationId,
                    Identifier = item.Pipeline.Identifier,
                    Name = item.Pipeline.Name,
                    Description = item.Pipeline.Description,
                    IsActive = item.Pipeline.IsActive,
                    CreatedAt = item.Pipeline.CreatedAt,
                    UpdatedAt = item.Pipeline.UpdatedAt,
                    Integration = item.Pipeline.Integration == null
                        ? null
                        : new Contracts.Integrations.IntegrationContract
                        {
                            Id = item.Pipeline.Integration.Id,
                            Identifier = item.Pipeline.Integration.Identifier,
                            Name = item.Pipeline.Integration.Name,
                            Description = item.Pipeline.Integration.Description,
                            IntegrationCategoryId = item.Pipeline.Integration.IntegrationCategoryId,
                            IsActive = item.Pipeline.Integration.IsActive,
                            CreatedAt = item.Pipeline.Integration.CreatedAt,
                            UpdatedAt = item.Pipeline.Integration.UpdatedAt,
                            IntegrationCategory = item.Pipeline.Integration.IntegrationCategory == null
                                ? null
                                : new Contracts.Integrations.IntegrationCategoryContract
                                {
                                    Id = item.Pipeline.Integration.IntegrationCategory.Id,
                                    Name = item.Pipeline.Integration.IntegrationCategory.Name,
                                    Description = item.Pipeline.Integration.IntegrationCategory.Description,
                                    IsActive = item.Pipeline.Integration.IntegrationCategory.IsActive,
                                    CreatedAt = item.Pipeline.Integration.IntegrationCategory.CreatedAt,
                                    UpdatedAt = item.Pipeline.Integration.IntegrationCategory.UpdatedAt
                                }
                        }
                },
            ApiCall = item.ApiCall == null
                ? null
                : new PipelineStepReferenceContract
                {
                    Id = item.ApiCall.Id,
                    Name = item.ApiCall.Name
                },
            JavaScriptFunction = item.JavaScriptFunction == null
                ? null
                : new PipelineStepReferenceContract
                {
                    Id = item.JavaScriptFunction.Id,
                    Name = item.JavaScriptFunction.Name
                },
            DatabaseScript = item.DatabaseScript == null
                ? null
                : new PipelineStepReferenceContract
                {
                    Id = item.DatabaseScript.Id,
                    Name = item.DatabaseScript.Name
                }
        };
    }

    public sealed class PipelineStepReferenceContract
    {
        public long Id { get; init; }

        public string Name { get; init; } = string.Empty;
    }
}
