namespace IntegrationPlataform.Application.Models
{
    public sealed class PipelineStepPreviewResult
    {
        public long PipelineStepId { get; init; }

        public string StepName { get; init; } = string.Empty;

        public string StepType { get; init; } = string.Empty;

        public string? Url { get; init; }

        public Dictionary<string, string> Headers { get; init; } = [];

        public string? Body { get; init; }

        public string? RequestExample { get; init; }

        public string? ResponseExample { get; init; }
    }
}
