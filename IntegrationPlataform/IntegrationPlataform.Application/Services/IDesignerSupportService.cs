using IntegrationPlataform.Application.Models;
using IntegrationPlataform.Application.Requests.Designer;

namespace IntegrationPlataform.Application.Services
{
    public interface IDesignerSupportService
    {
        Task<List<DesignerFieldSuggestion>> GetFieldSuggestions(long? connectorId = null, long? pipelineExampleId = null, long? pipelineStepExampleId = null, string? payloadExample = null, CancellationToken cancellationToken = default);

        Task<ApiCallPreviewResult> GenerateApiCallPreview(GenerateApiCallPreviewRequest request, CancellationToken cancellationToken = default);

        Task<PipelineStepPreviewResult> GeneratePipelineStepPreview(GeneratePipelineStepPreviewRequest request, CancellationToken cancellationToken = default);
    }
}
