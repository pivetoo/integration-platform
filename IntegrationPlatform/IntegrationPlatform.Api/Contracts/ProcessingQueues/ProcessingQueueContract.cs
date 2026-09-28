using IntegrationPlatform.Api.Contracts.Connectors;
using IntegrationPlatform.Api.Contracts.Pipelines;
using IntegrationPlatform.Domain.Entities;
using IntegrationPlatform.Domain.ValueObjects;
using System.Linq.Expressions;

namespace IntegrationPlatform.Api.Contracts.ProcessingQueues
{
    public sealed class ProcessingQueueContract
    {
        public long Id { get; init; }

        public int Priority { get; init; }

        public ProcessingStatus Status { get; init; }

        public string? Payload { get; init; }

        public string? LastError { get; init; }

        public int Attempts { get; init; }

        public string? IdempotencyKey { get; init; }

        public DateTimeOffset? ScheduledAt { get; init; }

        public DateTimeOffset? StartedAt { get; init; }

        public DateTimeOffset? FinishedAt { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }

        public ConnectorContract? Connector { get; init; }

        public PipelineContract? Pipeline { get; init; }

        public static Expression<Func<ProcessingQueue, ProcessingQueueContract>> Projection => item => new ProcessingQueueContract
        {
            Id = item.Id,
            Priority = item.Priority,
            Status = item.Status,
            Payload = item.Payload,
            LastError = item.LastError,
            Attempts = item.Attempts,
            IdempotencyKey = item.IdempotencyKey,
            ScheduledAt = item.ScheduledAt,
            StartedAt = item.StartedAt,
            FinishedAt = item.FinishedAt,
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
                }
        };
    }
}
