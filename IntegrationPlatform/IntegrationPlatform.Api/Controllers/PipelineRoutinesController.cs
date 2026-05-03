using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using IntegrationPlatform.Api.Contracts.PipelineRoutines;
using IntegrationPlatform.Application.Localization;
using IntegrationPlatform.Application.Requests.PipelineRoutines;
using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace IntegrationPlatform.Api.Controllers
{
    public sealed class PipelineRoutinesController : ApiControllerBase
    {
        private readonly IPipelineRoutineService pipelineRoutineService;
        private new IStringLocalizer<IntegrationPlatformResource> Localizer { get; }
        private static readonly Func<PipelineRoutine, PipelineRoutineContract> MapPipelineRoutine = PipelineRoutineContract.Projection.Compile();

        public PipelineRoutinesController(IPipelineRoutineService pipelineRoutineService, IStringLocalizer<IntegrationPlatformResource> localizer)
        {
            this.pipelineRoutineService = pipelineRoutineService;
            Localizer = localizer;
        }

        [RequireAccess("Permite listar as rotinas agendadas de pipelines.")]
        [GetEndpoint("[action]")]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            PagedResult<PipelineRoutine> result = await pipelineRoutineService.GetPipelineRoutines(request, cancellationToken);
            return Http200(new PagedResult<PipelineRoutineContract>
            {
                Items = result.Items.Select(MapPipelineRoutine).ToArray(),
                Pagination = result.Pagination
            });
        }

        [RequireAccess("Permite consultar os detalhes de uma rotina agendada de pipeline.")]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            PipelineRoutine? routine = await pipelineRoutineService.GetPipelineRoutineById(id, cancellationToken);

            return routine is null ? Http404(Localizer["pipeline.routine.notFound"]) : Http200(MapPipelineRoutine(routine));
        }

        [RequireAccess("Permite cadastrar uma nova rotina agendada para um pipeline.")]
        [PostEndpoint("[action]")]
        public async Task<IActionResult> Create([FromBody] CreatePipelineRoutineRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            PipelineRoutine routine = await pipelineRoutineService.CreatePipelineRoutine(request, cancellationToken);
            return Http201(MapPipelineRoutine(routine), Localizer["pipeline.routine.created"]);
        }

        [RequireAccess("Permite atualizar uma rotina agendada de pipeline.")]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdatePipelineRoutineRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            PipelineRoutine updatedRoutine = await pipelineRoutineService.UpdatePipelineRoutine(
                id,
                request.IntervalMinutes,
                request.IsActive,
                request.DefaultPayload,
                request.NextExecution,
                cancellationToken);

            return Http200(MapPipelineRoutine(updatedRoutine), Localizer["pipeline.routine.updated"]);
        }

        [RequireAccess("Permite excluir uma rotina agendada de pipeline.")]
        [DeleteEndpoint("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            PipelineRoutine? routine = await pipelineRoutineService.Delete(id, cancellationToken);
            if (routine is null)
            {
                return Http404(pipelineRoutineService.GetErrorMessages());
            }

            return Http200(MapPipelineRoutine(routine), Localizer["pipeline.routine.deleted"]);
        }
    }
}
