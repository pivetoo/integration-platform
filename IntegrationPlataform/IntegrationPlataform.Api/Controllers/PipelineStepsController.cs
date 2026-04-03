using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using IntegrationPlataform.Application.Requests.PipelineSteps;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Api.Controllers
{
    public sealed class PipelineStepsController : ReadOnlyController<PipelineStep>
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
        [PostEndpoint]
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
            return Http200(step, "Pipeline step updated successfully.");
        }
    }
}
