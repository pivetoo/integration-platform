using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using IntegrationPlataform.Api.Contracts.ProcessingQueues;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Api.Controllers
{
    [RequireAccess]
    public sealed class ProcessingQueuesController : ApiControllerBase
    {
        private readonly DbContext dbContext;
        private readonly IExecutionService executionService;
        private readonly IQueueProcessorService queueProcessorService;

        public ProcessingQueuesController(
            DbContext dbContext,
            IExecutionService executionService,
            IQueueProcessorService queueProcessorService)
        {
            this.dbContext = dbContext;
            this.executionService = executionService;
            this.queueProcessorService = queueProcessorService;
        }

        [GetEndpoint("")]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            var result = await dbContext.Set<ProcessingQueue>()
                .AsNoTracking()
                .OrderByDescending(item => item.CreatedAt)
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

            ProcessingQueue? item = await dbContext.Set<ProcessingQueue>()
                .AsNoTracking()
                .FirstOrDefaultAsync(current => current.Id == id, cancellationToken);

            return item is null ? Http404("Record not found.") : Http200(item);
        }

        [GetEndpoint("pending")]
        public async Task<IActionResult> GetPending(CancellationToken cancellationToken)
        {
            var items = await queueProcessorService.GetPendingToProcess(cancellationToken);
            return Http200(items);
        }

        [PostEndpoint("enqueue")]
        public async Task<IActionResult> Enqueue([FromBody] EnqueuePipelineRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            ProcessingQueue item = await executionService.EnqueuePipeline(
                request.ConnectorId,
                request.PipelineId,
                request.Payload,
                request.Priority,
                cancellationToken);

            return Http201(item, "Pipeline enqueued successfully.");
        }

        [PostEndpoint("{processingQueueId:long}/process")]
        public async Task<IActionResult> Process(long processingQueueId, CancellationToken cancellationToken)
        {
            if (processingQueueId <= 0)
            {
                return Http400("Processing queue id is required.");
            }

            await queueProcessorService.ProcessItem(processingQueueId, cancellationToken);
            return Http200(message: "Queue item processed successfully.");
        }
    }
}
