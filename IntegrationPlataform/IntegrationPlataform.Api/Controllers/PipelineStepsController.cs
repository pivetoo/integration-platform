using Archon.Api.Attributes;
using Archon.Core.Pagination;
using IntegrationPlataform.Application.Requests.PipelineSteps;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Api.Controllers
{
    public sealed class PipelineStepsController : IntegrationPlataformReadOnlyController<PipelineStep>
    {
        private readonly IPipelineStepService pipelineStepService;

        public PipelineStepsController(DbContext dbContext, IPipelineStepService pipelineStepService)
            : base(dbContext)
        {
            this.pipelineStepService = pipelineStepService;
        }

        [RequireAccess]
        [GetEndpoint]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            return await base.Get(request, cancellationToken);
        }

        [RequireAccess]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            return await base.GetById(id, cancellationToken);
        }

        [RequireAccess]
        [GetEndpoint("pipeline/{pipelineId:long}")]
        public async Task<IActionResult> GetByPipeline(long pipelineId, CancellationToken cancellationToken)
        {
            if (pipelineId <= 0)
            {
                return Http400(Localizer["request.pipeline.id.required"]);
            }

            List<PipelineStep> steps = await DbContext.Set<PipelineStep>()
                .AsNoTracking()
                .Where(item => item.PipelineId == pipelineId)
                .OrderBy(item => item.Order)
                .ToListAsync(cancellationToken);

            return Http200(steps);
        }

        [RequireAccess]
        [PostEndpoint]
        public async Task<IActionResult> Create([FromBody] CreatePipelineStepRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            PipelineStep step = await pipelineStepService.CreatePipelineStep(request, cancellationToken);
            return Http201(step, Localizer["pipeline.step.created"]);
        }

        [RequireAccess]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdatePipelineStepRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            PipelineStep step = await pipelineStepService.UpdatePipelineStep(id, request, cancellationToken);
            return Http200(step, Localizer["pipeline.step.updated"]);
        }

        [RequireAccess]
        [DeleteEndpoint("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            PipelineStep? step = await pipelineStepService.Delete(id, cancellationToken);
            if (step is null)
            {
                return Http404(pipelineStepService.GetErrorMessages());
            }

            return Http200(step, Localizer["pipeline.step.deleted"]);
        }
    }
}
