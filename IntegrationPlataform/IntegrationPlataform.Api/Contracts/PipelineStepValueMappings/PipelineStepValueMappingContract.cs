using IntegrationPlataform.Domain.Entities;
using System.Linq.Expressions;

namespace IntegrationPlataform.Api.Contracts.PipelineStepValueMappings
{
    public sealed class PipelineStepValueMappingContract
    {
        public long Id { get; init; }

        public long PipelineStepId { get; init; }

        public long? SourcePipelineStepId { get; init; }

        public long? PipelineExampleId { get; init; }

        public string TargetField { get; init; } = string.Empty;

        public string SourceType { get; init; } = string.Empty;

        public string SourcePath { get; init; } = string.Empty;

        public string ValueType { get; init; } = string.Empty;

        public string? FixedValue { get; init; }

        public int Order { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }

        public static Expression<Func<PipelineStepValueMapping, PipelineStepValueMappingContract>> Projection => item => new PipelineStepValueMappingContract
        {
            Id = item.Id,
            PipelineStepId = item.PipelineStepId,
            SourcePipelineStepId = item.SourcePipelineStepId,
            PipelineExampleId = item.PipelineExampleId,
            TargetField = item.TargetField,
            SourceType = item.SourceType,
            SourcePath = item.SourcePath,
            ValueType = item.ValueType,
            FixedValue = item.FixedValue,
            Order = item.Order,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }
}
