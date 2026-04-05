using Archon.Api.Attributes;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using IntegrationPlataform.Api.Contracts.PipelineRoutines;
using IntegrationPlataform.Application.Requests.PipelineRoutines;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Api.Controllers
{
    public sealed class PipelineRoutinesController : IntegrationPlataformReadOnlyController<PipelineRoutine>
    {
        private readonly IPipelineRoutineService pipelineRoutineService;

        public PipelineRoutinesController(DbContext dbContext, IPipelineRoutineService pipelineRoutineService) : base(dbContext)
        {
            this.pipelineRoutineService = pipelineRoutineService;
        }

        private IQueryable<PipelineRoutineContract> QueryContracts()
        {
            return DbContext.Set<PipelineRoutine>()
                .AsNoTracking()
                .Select(PipelineRoutineContract.Projection);
        }

        [RequireAccess]
        [GetEndpoint]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            var result = await QueryContracts()
                .OrderByDescending(item => item.CreatedAt)
                .ToPagedResultAsync(request, cancellationToken);

            return Http200(result);
        }

        [RequireAccess]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            PipelineRoutineContract? routine = await QueryContracts()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            return routine is null ? Http404(Localizer["pipeline.routine.notFound"]) : Http200(routine);
        }

        [RequireAccess]
        [PostEndpoint]
        public async Task<IActionResult> Create([FromBody] CreatePipelineRoutineRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            PipelineRoutine routine = new(
                request.ConnectorId,
                request.PipelineId,
                request.IntervalMinutes,
                request.IsActive,
                request.DefaultPayload);

            routine.Update(request.IntervalMinutes, request.IsActive, request.DefaultPayload, request.NextExecution);

            bool success = await pipelineRoutineService.Insert(cancellationToken, routine);
            if (!success)
            {
                return Http400(pipelineRoutineService.GetErrorMessages());
            }

            PipelineRoutineContract? contract = await QueryContracts()
                .FirstOrDefaultAsync(item => item.Id == routine.Id, cancellationToken);

            return Http201(contract ?? PipelineRoutineContract.Projection.Compile()(routine), Localizer["pipeline.routine.created"]);
        }

        [RequireAccess]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdatePipelineRoutineRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            PipelineRoutine? routine = await DbContext.Set<PipelineRoutine>()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (routine is null)
            {
                return Http404(Localizer["pipeline.routine.notFound"]);
            }

            routine.Update(request.IntervalMinutes, request.IsActive, request.DefaultPayload, request.NextExecution);

            PipelineRoutine? updatedRoutine = await pipelineRoutineService.Update(routine, cancellationToken);
            if (updatedRoutine is null)
            {
                return Http400(pipelineRoutineService.GetErrorMessages());
            }

            PipelineRoutineContract? contract = await QueryContracts()
                .FirstOrDefaultAsync(item => item.Id == updatedRoutine.Id, cancellationToken);

            return Http200(contract ?? PipelineRoutineContract.Projection.Compile()(updatedRoutine), Localizer["pipeline.routine.updated"]);
        }

        [RequireAccess]
        [DeleteEndpoint("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            PipelineRoutineContract? contract = await QueryContracts()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            PipelineRoutine? routine = await pipelineRoutineService.Delete(id, cancellationToken);
            if (routine is null)
            {
                return Http404(pipelineRoutineService.GetErrorMessages());
            }

            return Http200(contract ?? PipelineRoutineContract.Projection.Compile()(routine), Localizer["pipeline.routine.deleted"]);
        }
    }
}
