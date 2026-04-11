using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using IntegrationPlataform.Api.Contracts.PipelineExamples;
using IntegrationPlataform.Application.Localization;
using IntegrationPlataform.Application.Requests.PipelineExamples;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace IntegrationPlataform.Api.Controllers
{
    public sealed class PipelineExamplesController : ApiControllerBase
    {
        private readonly IPipelineExampleService pipelineExampleService;
        private static readonly Func<PipelineExample, PipelineExampleContract> MapPipelineExample = PipelineExampleContract.Projection.Compile();
        private new IStringLocalizer<IntegrationPlataformResource> Localizer { get; }

        public PipelineExamplesController(IPipelineExampleService pipelineExampleService, IStringLocalizer<IntegrationPlataformResource> localizer)
        {
            this.pipelineExampleService = pipelineExampleService;
            Localizer = localizer;
        }

        [RequireAccess("Permite listar os exemplos cadastrados para os pipelines da plataforma.")]
        [GetEndpoint("[action]")]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            PagedResult<PipelineExample> result = await pipelineExampleService.GetPipelineExamples(request, cancellationToken);
            return Http200(new PagedResult<PipelineExampleContract>
            {
                Items = result.Items.Select(MapPipelineExample).ToArray(),
                Pagination = result.Pagination
            });
        }

        [RequireAccess("Permite consultar os detalhes de um exemplo de pipeline específico.")]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            PipelineExample? entity = await pipelineExampleService.GetPipelineExampleById(id, cancellationToken);
            return entity is null ? Http404(Localizer["pipelineExample.notFound"]) : Http200(MapPipelineExample(entity));
        }

        [RequireAccess("Permite listar os exemplos vinculados a um pipeline específico.")]
        [GetEndpoint("pipeline/{pipelineId:long}")]
        public async Task<IActionResult> GetByPipeline(long pipelineId, CancellationToken cancellationToken)
        {
            if (pipelineId <= 0)
            {
                return Http400(Localizer["request.pipeline.id.required"]);
            }

            List<PipelineExample> entities = await pipelineExampleService.GetByPipeline(pipelineId, cancellationToken);
            return Http200(entities.Select(MapPipelineExample).ToList());
        }

        [RequireAccess("Permite cadastrar um novo exemplo para um pipeline da plataforma.")]
        [PostEndpoint("[action]")]
        public async Task<IActionResult> Create([FromBody] CreatePipelineExampleRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            PipelineExample entity = await pipelineExampleService.CreatePipelineExample(request, cancellationToken);
            return Http201(MapPipelineExample(entity), Localizer["pipelineExample.created"]);
        }

        [RequireAccess("Permite atualizar um exemplo de pipeline cadastrado.")]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdatePipelineExampleRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            PipelineExample entity = await pipelineExampleService.UpdatePipelineExample(id, request, cancellationToken);
            return Http200(MapPipelineExample(entity), Localizer["pipelineExample.updated"]);
        }

        [RequireAccess("Permite excluir um exemplo de pipeline cadastrado na plataforma.")]
        [DeleteEndpoint("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            PipelineExample? entity = await pipelineExampleService.Delete(id, cancellationToken);
            if (entity is null)
            {
                return Http404(pipelineExampleService.GetErrorMessages());
            }

            return Http200(MapPipelineExample(entity), Localizer["pipelineExample.deleted"]);
        }
    }
}
