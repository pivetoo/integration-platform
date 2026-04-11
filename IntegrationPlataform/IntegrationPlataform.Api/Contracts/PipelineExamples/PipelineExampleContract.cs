using IntegrationPlataform.Domain.Entities;
using System.Linq.Expressions;

namespace IntegrationPlataform.Api.Contracts.PipelineExamples
{
    public sealed class PipelineExampleContract
    {
        public long Id { get; init; }

        public long PipelineId { get; init; }

        public string Name { get; init; } = string.Empty;

        public string? Description { get; init; }

        public string? InputPayloadExample { get; init; }

        public string? ExpectedOutputExample { get; init; }

        public bool IsDefault { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }

        public static Expression<Func<PipelineExample, PipelineExampleContract>> Projection => item => new PipelineExampleContract
        {
            Id = item.Id,
            PipelineId = item.PipelineId,
            Name = item.Name,
            Description = item.Description,
            InputPayloadExample = item.InputPayloadExample,
            ExpectedOutputExample = item.ExpectedOutputExample,
            IsDefault = item.IsDefault,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }
}
