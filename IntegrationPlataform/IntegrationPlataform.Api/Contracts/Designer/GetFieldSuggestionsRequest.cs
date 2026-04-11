using System.ComponentModel.DataAnnotations;

namespace IntegrationPlataform.Api.Contracts.Designer
{
    public sealed class GetFieldSuggestionsRequest
    {
        public long? ConnectorId { get; set; }

        public long? PipelineExampleId { get; set; }

        public long? PipelineStepExampleId { get; set; }

        public string? PayloadExample { get; set; }
    }
}
