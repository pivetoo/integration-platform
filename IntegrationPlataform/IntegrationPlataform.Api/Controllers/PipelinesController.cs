using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using IntegrationPlataform.Api.Contracts.Pipelines;
using IntegrationPlataform.Application.Localization;
using IntegrationPlataform.Application.Requests.Pipelines;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace IntegrationPlataform.Api.Controllers
{
    public sealed class PipelinesController : ApiControllerBase
    {
        private readonly IPipelineService pipelineService;
        private new IStringLocalizer<IntegrationPlataformResource> Localizer { get; }
        private static readonly Func<Pipeline, PipelineContract> MapPipeline = PipelineContract.Projection.Compile();

        public PipelinesController(IPipelineService pipelineService, IStringLocalizer<IntegrationPlataformResource> localizer)
        {
            this.pipelineService = pipelineService;
            Localizer = localizer;
        }

        [RequireAccess]
        [GetEndpoint]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            PagedResult<Pipeline> result = await pipelineService.GetPipelines(request, cancellationToken);
            return Http200(new PagedResult<PipelineContract>
            {
                Items = result.Items.Select(MapPipeline).ToArray(),
                Pagination = result.Pagination
            });
        }

        [RequireAccess]
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

        [RequireAccess]
        [GetEndpoint("active")]
        public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
        {
            List<Pipeline> pipelines = await pipelineService.GetActivePipelines(cancellationToken);

            return Http200(pipelines.Select(MapPipeline).ToList());
        }

        [RequireAccess]
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

        [RequireAccess]
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

        [RequireAccess]
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
