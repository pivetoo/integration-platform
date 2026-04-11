using System.ComponentModel.DataAnnotations;

namespace IntegrationPlataform.Application.Requests.Designer
{
    public sealed class GenerateApiCallPreviewRequest
    {
        [Range(1, long.MaxValue)]
        public long ApiCallId { get; set; }

        public long? ConnectorId { get; set; }

        public long? PipelineExampleId { get; set; }

        public long? PipelineStepExampleId { get; set; }

        public string? PayloadExample { get; set; }
    }
}
