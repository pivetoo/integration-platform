using IntegrationPlataform.Api.Contracts.Connectors;
using IntegrationPlataform.Api.Contracts.Integrations;
using IntegrationPlataform.Api.Contracts.Pipelines;
using IntegrationPlataform.Domain.Entities;
using IntegrationPlataform.Domain.ValueObjects;
using System.Linq.Expressions;
using ExecutionEntity = IntegrationPlataform.Domain.Entities.Execution;

namespace IntegrationPlataform.Api.Contracts.Execution
{
    public sealed class ExecutionContract
    {
        public long Id { get; init; }

        public ExecutionType Type { get; init; }

        public long ConnectorId { get; init; }

        public long? PipelineId { get; init; }

        public ExecutionStatus Status { get; init; }

        public string? InputData { get; init; }

        public string? OutputData { get; init; }

        public string? Errors { get; init; }

        public DateTimeOffset StartedAt { get; init; }

        public DateTimeOffset? FinishedAt { get; init; }

        public long? Duration { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }

        public ConnectorContract? Connector { get; init; }

        public PipelineContract? Pipeline { get; init; }

        public static Expression<Func<ExecutionEntity, ExecutionContract>> Projection => item => new ExecutionContract
        {
            Id = item.Id,
            Type = item.Type,
            ConnectorId = item.ConnectorId,
            PipelineId = item.PipelineId,
            Status = item.Status,
            InputData = item.InputData,
            OutputData = item.OutputData,
            Errors = item.Errors,
            StartedAt = item.StartedAt,
            FinishedAt = item.FinishedAt,
            Duration = item.Duration,
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
                        : new IntegrationContract
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
                                : new IntegrationCategoryContract
                                {
                                    Id = item.Connector.Integration.IntegrationCategory.Id,
                                    Name = item.Connector.Integration.IntegrationCategory.Name,
                                    Description = item.Connector.Integration.IntegrationCategory.Description,
                                    IsActive = item.Connector.Integration.IntegrationCategory.IsActive,
                                    CreatedAt = item.Connector.Integration.IntegrationCategory.CreatedAt,
                                    UpdatedAt = item.Connector.Integration.IntegrationCategory.UpdatedAt
                                }
                        }
                },
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
                        : new IntegrationContract
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
                                : new IntegrationCategoryContract
                                {
                                    Id = item.Pipeline.Integration.IntegrationCategory.Id,
                                    Name = item.Pipeline.Integration.IntegrationCategory.Name,
                                    Description = item.Pipeline.Integration.IntegrationCategory.Description,
                                    IsActive = item.Pipeline.Integration.IntegrationCategory.IsActive,
                                    CreatedAt = item.Pipeline.Integration.IntegrationCategory.CreatedAt,
                                    UpdatedAt = item.Pipeline.Integration.IntegrationCategory.UpdatedAt
                                }
                        }
                }
        };
    }
}
