using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using IntegrationPlatform.Api.Contracts.PipelineSteps;
using IntegrationPlatform.Application.Localization;
using IntegrationPlatform.Application.Requests.PipelineSteps;
using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace IntegrationPlatform.Api.Controllers
{
    public sealed class PipelineStepsController : ApiControllerBase
    {
        private readonly IPipelineStepService pipelineStepService;
        private new IStringLocalizer<IntegrationPlatformResource> Localizer { get; }
        private static readonly Func<PipelineStep, PipelineStepContract> MapPipelineStep = PipelineStepContract.Projection.Compile();

        public PipelineStepsController(IPipelineStepService pipelineStepService, IStringLocalizer<IntegrationPlatformResource> localizer)
        {
            this.pipelineStepService = pipelineStepService;
            Localizer = localizer;
        }

        [RequireAccess("Permite listar as etapas de pipeline cadastradas na plataforma.")]
        [GetEndpoint("[action]")]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            PagedResult<PipelineStep> result = await pipelineStepService.GetPipelineSteps(request, cancellationToken);
            return Http200(new PagedResult<PipelineStepContract>
            {
                Items = result.Items.Select(MapPipelineStep).ToArray(),
                Pagination = result.Pagination
            });
        }

        [RequireAccess("Permite consultar os detalhes de uma etapa de pipeline específica.")]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            PipelineStep? step = await pipelineStepService.GetPipelineStepById(id, cancellationToken);

            return step is null ? Http404(Localizer["pipeline.step.notFound"]) : Http200(MapPipelineStep(step));
        }

        [RequireAccess("Permite listar as etapas vinculadas a um pipeline específico.")]
        [GetEndpoint("pipeline/{pipelineId:long}")]
        public async Task<IActionResult> GetByPipeline(long pipelineId, CancellationToken cancellationToken)
        {
            if (pipelineId <= 0)
            {
                return Http400(Localizer["request.pipeline.id.required"]);
            }

            List<PipelineStep> steps = await pipelineStepService.GetPipelineStepsByPipeline(pipelineId, cancellationToken);

            return Http200(steps.Select(MapPipelineStep).ToList());
        }

        [RequireAccess("Permite cadastrar uma nova etapa em um pipeline.")]
        [PostEndpoint("[action]")]
        public async Task<IActionResult> Create([FromBody] CreatePipelineStepRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            PipelineStep step = await pipelineStepService.CreatePipelineStep(request, cancellationToken);
            return Http201(MapPipelineStep(step), Localizer["pipeline.step.created"]);
        }

        [RequireAccess("Permite atualizar a configuração de uma etapa de pipeline.")]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdatePipelineStepRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            PipelineStep step = await pipelineStepService.UpdatePipelineStep(id, request, cancellationToken);
            return Http200(MapPipelineStep(step), Localizer["pipeline.step.updated"]);
        }

        [RequireAccess("Permite excluir uma etapa cadastrada em um pipeline.")]
        [DeleteEndpoint("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            PipelineStep? step = await pipelineStepService.Delete(id, cancellationToken);
            if (step is null)
            {
                return Http404(pipelineStepService.GetErrorMessages());
            }

            return Http200(MapPipelineStep(step), Localizer["pipeline.step.deleted"]);
        }
    }
}
