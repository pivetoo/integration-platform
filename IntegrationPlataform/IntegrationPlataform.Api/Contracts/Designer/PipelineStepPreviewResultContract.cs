using IntegrationPlataform.Application.Models;

namespace IntegrationPlataform.Api.Contracts.Designer
{
    public sealed class PipelineStepPreviewResultContract
    {
        public long PipelineStepId { get; init; }

        public string StepName { get; init; } = string.Empty;

        public string StepType { get; init; } = string.Empty;

        public string? Url { get; init; }

        public Dictionary<string, string> Headers { get; init; } = [];

        public string? Body { get; init; }

        public string? RequestExample { get; init; }

        public string? ResponseExample { get; init; }

        public static PipelineStepPreviewResultContract FromModel(PipelineStepPreviewResult model)
        {
            return new PipelineStepPreviewResultContract
            {
                PipelineStepId = model.PipelineStepId,
                StepName = model.StepName,
                StepType = model.StepType,
                Url = model.Url,
                Headers = model.Headers,
                Body = model.Body,
                RequestExample = model.RequestExample,
                ResponseExample = model.ResponseExample
            };
        }
    }
}
