using IntegrationPlataform.Domain.Entities;
using System.Linq.Expressions;

namespace IntegrationPlataform.Api.Contracts.PipelineStepExamples
{
    public sealed class PipelineStepExampleContract
    {
        public long Id { get; init; }

        public long PipelineStepId { get; init; }

        public long? PipelineExampleId { get; init; }

        public string Name { get; init; } = string.Empty;

        public string? RequestExample { get; init; }

        public string? ResponseExample { get; init; }

        public bool IsDefault { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }

        public static Expression<Func<PipelineStepExample, PipelineStepExampleContract>> Projection => item => new PipelineStepExampleContract
        {
            Id = item.Id,
            PipelineStepId = item.PipelineStepId,
            PipelineExampleId = item.PipelineExampleId,
            Name = item.Name,
            RequestExample = item.RequestExample,
            ResponseExample = item.ResponseExample,
            IsDefault = item.IsDefault,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }
}
