using IntegrationPlataform.Application.Models;

namespace IntegrationPlataform.Api.Contracts.Designer
{
    public sealed class FieldSuggestionContract
    {
        public string Key { get; init; } = string.Empty;

        public string Label { get; init; } = string.Empty;

        public string Source { get; init; } = string.Empty;

        public string? SourceName { get; init; }

        public string Type { get; init; } = string.Empty;

        public string? ExampleValue { get; init; }

        public long? PipelineStepId { get; init; }

        public long? PipelineExampleId { get; init; }

        public static FieldSuggestionContract FromModel(DesignerFieldSuggestion model)
        {
            return new FieldSuggestionContract
            {
                Key = model.Key,
                Label = model.Label,
                Source = model.Source,
                SourceName = model.SourceName,
                Type = model.Type,
                ExampleValue = model.ExampleValue,
                PipelineStepId = model.PipelineStepId,
                PipelineExampleId = model.PipelineExampleId
            };
        }
    }
}
