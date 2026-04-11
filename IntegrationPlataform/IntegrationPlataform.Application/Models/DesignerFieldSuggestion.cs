namespace IntegrationPlataform.Application.Models
{
    public sealed class DesignerFieldSuggestion
    {
        public string Key { get; init; } = string.Empty;

        public string Label { get; init; } = string.Empty;

        public string Source { get; init; } = string.Empty;

        public string? SourceName { get; init; }

        public string Type { get; init; } = string.Empty;

        public string? ExampleValue { get; init; }

        public long? PipelineStepId { get; init; }

        public long? PipelineExampleId { get; init; }
    }
}
