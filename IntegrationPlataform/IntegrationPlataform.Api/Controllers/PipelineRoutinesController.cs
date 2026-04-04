using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Api.Controllers
{
    public sealed class PipelineRoutinesController : ReadOnlyController<PipelineRoutine>
    {
        private readonly IPipelineRoutineService pipelineRoutineService;

        public PipelineRoutinesController(DbContext dbContext, IPipelineRoutineService pipelineRoutineService) : base(dbContext)
        {
            this.pipelineRoutineService = pipelineRoutineService;
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
        [DeleteEndpoint("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            PipelineRoutine? routine = await pipelineRoutineService.Delete(id, cancellationToken);
            if (routine is null)
            {
                return Http404(pipelineRoutineService.GetErrorMessages());
            }

            return Http200(routine, "Pipeline routine deleted successfully.");
        }
    }
}
