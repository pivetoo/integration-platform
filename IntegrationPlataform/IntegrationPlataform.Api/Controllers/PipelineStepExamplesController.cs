using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using IntegrationPlataform.Api.Contracts.PipelineStepExamples;
using IntegrationPlataform.Application.Localization;
using IntegrationPlataform.Application.Requests.PipelineStepExamples;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace IntegrationPlataform.Api.Controllers
{
    public sealed class PipelineStepExamplesController : ApiControllerBase
    {
        private readonly IPipelineStepExampleService pipelineStepExampleService;
        private static readonly Func<PipelineStepExample, PipelineStepExampleContract> MapPipelineStepExample = PipelineStepExampleContract.Projection.Compile();
        private new IStringLocalizer<IntegrationPlataformResource> Localizer { get; }

        public PipelineStepExamplesController(IPipelineStepExampleService pipelineStepExampleService, IStringLocalizer<IntegrationPlataformResource> localizer)
        {
            this.pipelineStepExampleService = pipelineStepExampleService;
            Localizer = localizer;
        }

        [RequireAccess("Permite listar os exemplos cadastrados para as etapas dos pipelines.")]
        [GetEndpoint("[action]")]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            PagedResult<PipelineStepExample> result = await pipelineStepExampleService.GetPipelineStepExamples(request, cancellationToken);
            return Http200(new PagedResult<PipelineStepExampleContract>
            {
                Items = result.Items.Select(MapPipelineStepExample).ToArray(),
                Pagination = result.Pagination
            });
        }

        [RequireAccess("Permite consultar os detalhes de um exemplo de etapa específico.")]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            PipelineStepExample? entity = await pipelineStepExampleService.GetPipelineStepExampleById(id, cancellationToken);
            return entity is null ? Http404(Localizer["pipelineStepExample.notFound"]) : Http200(MapPipelineStepExample(entity));
        }

        [RequireAccess("Permite listar os exemplos vinculados a uma etapa específica do pipeline.")]
        [GetEndpoint("pipeline-step/{pipelineStepId:long}")]
        public async Task<IActionResult> GetByPipelineStep(long pipelineStepId, CancellationToken cancellationToken)
        {
            if (pipelineStepId <= 0)
            {
                return Http400(Localizer["request.pipelineStep.id.required"]);
            }

            List<PipelineStepExample> entities = await pipelineStepExampleService.GetByPipelineStep(pipelineStepId, cancellationToken);
            return Http200(entities.Select(MapPipelineStepExample).ToList());
        }

        [RequireAccess("Permite cadastrar um novo exemplo para uma etapa do pipeline.")]
        [PostEndpoint("[action]")]
        public async Task<IActionResult> Create([FromBody] CreatePipelineStepExampleRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            PipelineStepExample entity = await pipelineStepExampleService.CreatePipelineStepExample(request, cancellationToken);
            return Http201(MapPipelineStepExample(entity), Localizer["pipelineStepExample.created"]);
        }

        [RequireAccess("Permite atualizar um exemplo de etapa cadastrado.")]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdatePipelineStepExampleRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            PipelineStepExample entity = await pipelineStepExampleService.UpdatePipelineStepExample(id, request, cancellationToken);
            return Http200(MapPipelineStepExample(entity), Localizer["pipelineStepExample.updated"]);
        }

        [RequireAccess("Permite excluir um exemplo de etapa cadastrado na plataforma.")]
        [DeleteEndpoint("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            PipelineStepExample? entity = await pipelineStepExampleService.Delete(id, cancellationToken);
            if (entity is null)
            {
                return Http404(pipelineStepExampleService.GetErrorMessages());
            }

            return Http200(MapPipelineStepExample(entity), Localizer["pipelineStepExample.deleted"]);
        }
    }
}
