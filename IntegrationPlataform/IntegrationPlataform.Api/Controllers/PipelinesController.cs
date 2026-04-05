using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using IntegrationPlataform.Api.Contracts.Pipelines;
using IntegrationPlataform.Application.Requests.Pipelines;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Api.Controllers
{
    public sealed class PipelinesController : ReadOnlyController<Pipeline>
    {
        private readonly IPipelineService pipelineService;

        public PipelinesController(DbContext dbContext, IPipelineService pipelineService)
            : base(dbContext)
        {
            this.pipelineService = pipelineService;
        }

        private IQueryable<PipelineContract> QueryContracts()
        {
            return DbContext.Set<Pipeline>()
                .AsNoTracking()
                .Select(PipelineContract.Projection);
        }

        [RequireAccess]
        [GetEndpoint]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            var result = await QueryContracts()
                .OrderBy(item => item.Name)
                .ToPagedResultAsync(request, cancellationToken);

            return Http200(result);
        }

        [RequireAccess]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            var pipeline = await QueryContracts()
                .Where(item => item.Id == id)
                .FirstOrDefaultAsync(cancellationToken);

            return pipeline is null ? Http404("Record not found.") : Http200(pipeline);
        }

        [RequireAccess]
        [GetEndpoint("integration/{integrationId:long}")]
        public async Task<IActionResult> GetByIntegration(long integrationId, CancellationToken cancellationToken)
        {
            if (integrationId <= 0)
            {
                return Http400("Integration id is required.");
            }

            var pipelines = await QueryContracts()
                .Where(item => item.IntegrationId == integrationId)
                .OrderBy(item => item.Name)
                .ToListAsync(cancellationToken);

            return Http200(pipelines);
        }

        [RequireAccess]
        [GetEndpoint("active")]
        public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
        {
            var pipelines = await QueryContracts()
                .Where(item => item.IsActive)
                .OrderBy(item => item.Name)
                .ToListAsync(cancellationToken);

            return Http200(pipelines);
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
            PipelineContract? contract = await QueryContracts()
                .FirstOrDefaultAsync(item => item.Id == pipeline.Id, cancellationToken);

            return Http201(contract ?? PipelineContract.Projection.Compile()(pipeline), "Pipeline created successfully.");
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
            PipelineContract? contract = await QueryContracts()
                .FirstOrDefaultAsync(item => item.Id == pipeline.Id, cancellationToken);

            return Http200(contract ?? PipelineContract.Projection.Compile()(pipeline), "Pipeline updated successfully.");
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

            return Http200(pipeline, "Pipeline deleted successfully.");
        }
    }
}
