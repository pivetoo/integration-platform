using Archon.Api.Attributes;
using Archon.Api.Controllers;
using IntegrationPlataform.Api.Contracts.Designer;
using IntegrationPlataform.Application.Requests.Designer;
using IntegrationPlataform.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace IntegrationPlataform.Api.Controllers
{
    public sealed class DesignerController : ApiControllerBase
    {
        private readonly IDesignerSupportService designerSupportService;

        public DesignerController(IDesignerSupportService designerSupportService)
        {
            this.designerSupportService = designerSupportService;
        }

        [RequireAccess("Permite obter sugestões de campos para a experiência guiada de configuração.")]
        [GetEndpoint("field-suggestions")]
        public async Task<IActionResult> GetFieldSuggestions([FromQuery] GetFieldSuggestionsRequest request, CancellationToken cancellationToken)
        {
            List<FieldSuggestionContract> result = (await designerSupportService.GetFieldSuggestions(
                request.ConnectorId,
                request.PipelineExampleId,
                request.PipelineStepExampleId,
                request.PayloadExample,
                cancellationToken))
                .Select(FieldSuggestionContract.FromModel)
                .ToList();

            return Http200(result);
        }

        [RequireAccess("Permite gerar preview de uma chamada de API com base em exemplos e parâmetros configurados.")]
        [PostEndpoint("api-call-preview")]
        public async Task<IActionResult> GenerateApiCallPreview([FromBody] GenerateApiCallPreviewRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            ApiCallPreviewResultContract result = ApiCallPreviewResultContract.FromModel(
                await designerSupportService.GenerateApiCallPreview(request, cancellationToken));

            return Http200(result);
        }

        [RequireAccess("Permite gerar preview de uma etapa do pipeline com base em exemplos, mapeamentos e configurações técnicas.")]
        [PostEndpoint("pipeline-step-preview")]
        public async Task<IActionResult> GeneratePipelineStepPreview([FromBody] GeneratePipelineStepPreviewRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            PipelineStepPreviewResultContract result = PipelineStepPreviewResultContract.FromModel(
                await designerSupportService.GeneratePipelineStepPreview(request, cancellationToken));

            return Http200(result);
        }
    }
}
