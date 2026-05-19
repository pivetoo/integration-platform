using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using IntegrationPlatform.Api.Contracts.Pipelines;
using IntegrationPlatform.Application.Localization;
using IntegrationPlatform.Application.Requests.Pipelines;
using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace IntegrationPlatform.Api.Controllers
{
    public sealed class PipelinesController : ApiControllerBase
    {
        private readonly IPipelineService pipelineService;
        private new IStringLocalizer<IntegrationPlatformResource> Localizer { get; }
        private static readonly Func<Pipeline, PipelineContract> MapPipeline = PipelineContract.Projection.Compile();

        public PipelinesController(IPipelineService pipelineService, IStringLocalizer<IntegrationPlatformResource> localizer)
        {
            this.pipelineService = pipelineService;
            Localizer = localizer;
        }

        [RequireAccess("Permite listar os pipelines cadastrados na plataforma.")]
        [GetEndpoint]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, [FromQuery] string? search, CancellationToken cancellationToken)
        {
            PagedResult<Pipeline> result = await pipelineService.GetPipelines(request, search, cancellationToken);
            return Http200(new PagedResult<PipelineContract>
            {
                Items = result.Items.Select(MapPipeline).ToArray(),
                Pagination = result.Pagination
            });
        }

        [RequireAccess("Permite consultar os detalhes de um pipeline específico.")]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            Pipeline? pipeline = await pipelineService.GetPipelineById(id, cancellationToken);

            return pipeline is null ? Http404(Localizer["pipeline.notFound"]) : Http200(MapPipeline(pipeline));
        }

        [RequireAccess]
        [GetEndpoint("integration/{integrationId:long}")]
        public async Task<IActionResult> GetByIntegration(long integrationId, CancellationToken cancellationToken)
        {
            if (integrationId <= 0)
            {
                return Http400(Localizer["request.integration.id.required"]);
            }

            List<Pipeline> pipelines = await pipelineService.GetPipelinesByIntegration(integrationId, cancellationToken);

            return Http200(pipelines.Select(MapPipeline).ToList());
        }

        [RequireAccess("Permite listar apenas os pipelines ativos.")]
        [GetEndpoint("active")]
        public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
        {
            List<Pipeline> pipelines = await pipelineService.GetActivePipelines(cancellationToken);

            return Http200(pipelines.Select(MapPipeline).ToList());
        }

        [RequireAccess("Permite cadastrar um novo pipeline para uma integração.")]
        [PostEndpoint]
        public async Task<IActionResult> Create([FromBody] CreatePipelineRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            Pipeline pipeline = await pipelineService.CreatePipeline(request, cancellationToken);
            return Http201(MapPipeline(pipeline), Localizer["pipeline.created"]);
        }

        [RequireAccess("Permite atualizar a configuração de um pipeline cadastrado.")]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdatePipelineRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            Pipeline pipeline = await pipelineService.UpdatePipeline(id, request, cancellationToken);
            return Http200(MapPipeline(pipeline), Localizer["pipeline.updated"]);
        }

        [RequireAccess("Permite definir um pipeline como padrão para sua integração.")]
        [PutEndpoint("{id:long}/[action]")]
        public async Task<IActionResult> SetDefault(long id, CancellationToken cancellationToken)
        {
            Pipeline pipeline = await pipelineService.SetDefaultPipeline(id, cancellationToken);
            return Http200(MapPipeline(pipeline), Localizer["pipeline.updated"]);
        }

        [RequireAccess("Permite excluir um pipeline cadastrado na plataforma.")]
        [DeleteEndpoint("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            Pipeline? pipeline = await pipelineService.Delete(id, cancellationToken);
            if (pipeline is null)
            {
                return Http404(pipelineService.GetErrorMessages());
            }

            return Http200(MapPipeline(pipeline), Localizer["pipeline.deleted"]);
        }
    }
}
