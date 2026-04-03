using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using IntegrationPlataform.Application.Requests.PipelineSteps;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Api.Controllers
{
    [RequireAccess]
    public sealed class PipelineStepsController : ApiControllerBase
    {
        private readonly DbContext dbContext;
        private readonly IPipelineStepService pipelineStepService;

        public PipelineStepsController(DbContext dbContext, IPipelineStepService pipelineStepService)
        {
            this.dbContext = dbContext;
            this.pipelineStepService = pipelineStepService;
        }

        [GetEndpoint("")]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            var result = await dbContext.Set<PipelineStep>()
                .AsNoTracking()
                .OrderByDescending(item => item.Id)
                .ToPagedResultAsync(request, cancellationToken);

            return Http200(result);
        }

        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return Http400("Id is required.");
            }

            PipelineStep? entity = await dbContext.Set<PipelineStep>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            return entity is null ? Http404("Record not found.") : Http200(entity);
        }

        [PostEndpoint("")]
        public async Task<IActionResult> Create([FromBody] CreatePipelineStepRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            PipelineStep step = await pipelineStepService.CreatePipelineStep(request, cancellationToken);
            return Http201(step, "Pipeline step created successfully.");
        }

        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdatePipelineStepRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            PipelineStep step = await pipelineStepService.UpdatePipelineStep(id, request, cancellationToken);
            return Http200(step, "Pipeline step updated successfully.");
        }
    }
}
