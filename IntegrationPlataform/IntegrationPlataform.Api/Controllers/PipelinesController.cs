using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using IntegrationPlataform.Application.Requests.Pipelines;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Api.Controllers
{
    [RequireAccess]
    public sealed class PipelinesController : ApiControllerBase
    {
        private readonly DbContext dbContext;
        private readonly IPipelineService pipelineService;

        public PipelinesController(DbContext dbContext, IPipelineService pipelineService)
        {
            this.dbContext = dbContext;
            this.pipelineService = pipelineService;
        }

        [GetEndpoint("")]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            var result = await dbContext.Set<Pipeline>()
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

            Pipeline? entity = await dbContext.Set<Pipeline>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            return entity is null ? Http404("Record not found.") : Http200(entity);
        }

        [PostEndpoint("")]
        public async Task<IActionResult> Create([FromBody] CreatePipelineRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            Pipeline pipeline = await pipelineService.CreatePipeline(request, cancellationToken);
            return Http201(pipeline, "Pipeline created successfully.");
        }

        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdatePipelineRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            Pipeline pipeline = await pipelineService.UpdatePipeline(id, request, cancellationToken);
            return Http200(pipeline, "Pipeline updated successfully.");
        }
    }
}
