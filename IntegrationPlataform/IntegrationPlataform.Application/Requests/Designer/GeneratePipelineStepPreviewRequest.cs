using System.ComponentModel.DataAnnotations;

namespace IntegrationPlataform.Application.Requests.Designer
{
    public sealed class GeneratePipelineStepPreviewRequest
    {
        [Range(1, long.MaxValue)]
        public long PipelineStepId { get; set; }

        public long? ConnectorId { get; set; }

        public long? PipelineExampleId { get; set; }

        public string? PayloadExample { get; set; }
    }
}
