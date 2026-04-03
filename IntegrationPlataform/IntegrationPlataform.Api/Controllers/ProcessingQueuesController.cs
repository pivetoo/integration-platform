using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using IntegrationPlataform.Api.Contracts.ProcessingQueues;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Api.Controllers
{
    public sealed class ProcessingQueuesController : ReadOnlyController<ProcessingQueue>
    {
        private readonly IExecutionService executionService;
        private readonly IQueueProcessorService queueProcessorService;

        public ProcessingQueuesController(DbContext dbContext, IExecutionService executionService, IQueueProcessorService queueProcessorService) : base(dbContext)
        {
            this.executionService = executionService;
            this.queueProcessorService = queueProcessorService;
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
        [GetEndpoint("pending")]
        public async Task<IActionResult> GetPending(CancellationToken cancellationToken)
        {
            var items = await queueProcessorService.GetPendingToProcess(cancellationToken);
            return Http200(items);
        }

        [RequireAccess]
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

        [RequireAccess]
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
