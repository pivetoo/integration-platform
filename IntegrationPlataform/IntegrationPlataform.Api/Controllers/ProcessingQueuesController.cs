using Archon.Api.Attributes;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using IntegrationPlataform.Api.Contracts.ProcessingQueues;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Api.Controllers
{
    public sealed class ProcessingQueuesController : IntegrationPlataformReadOnlyController<ProcessingQueue>
    {
        private readonly IExecutionService executionService;
        private readonly IProcessingQueueService processingQueueService;
        private readonly IQueueProcessorService queueProcessorService;

        public ProcessingQueuesController(
            DbContext dbContext,
            IExecutionService executionService,
            IProcessingQueueService processingQueueService,
            IQueueProcessorService queueProcessorService) : base(dbContext)
        {
            this.executionService = executionService;
            this.processingQueueService = processingQueueService;
            this.queueProcessorService = queueProcessorService;
        }

        private IQueryable<ProcessingQueueContract> QueryContracts()
        {
            return DbContext.Set<ProcessingQueue>()
                .AsNoTracking()
                .Select(ProcessingQueueContract.Projection);
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
            ProcessingQueueContract? item = await QueryContracts()
                .FirstOrDefaultAsync(current => current.Id == id, cancellationToken);

            return item is null ? Http404(Localizer["processingQueue.notFound"]) : Http200(item);
        }

        [RequireAccess]
        [GetEndpoint("pending")]
        public async Task<IActionResult> GetPending(CancellationToken cancellationToken)
        {
            var items = await QueryContracts()
                .Where(item => item.Status == Domain.ValueObjects.ProcessingStatus.Pending &&
                    (!item.ScheduledAt.HasValue || item.ScheduledAt <= DateTimeOffset.UtcNow))
                .OrderBy(item => item.Priority)
                .ThenBy(item => item.CreatedAt)
                .ToListAsync(cancellationToken);

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

            ProcessingQueueContract? contract = await QueryContracts()
                .FirstOrDefaultAsync(current => current.Id == item.Id, cancellationToken);

            return Http201(contract ?? ProcessingQueueContract.Projection.Compile()(item), Localizer["processingQueue.enqueued"]);
        }

        [RequireAccess]
        [PostEndpoint("{processingQueueId:long}/process")]
        public async Task<IActionResult> Process(long processingQueueId, CancellationToken cancellationToken)
        {
            if (processingQueueId <= 0)
            {
                return Http400(Localizer["request.processingQueue.id.required"]);
            }

            await queueProcessorService.ProcessItem(processingQueueId, cancellationToken);
            return Http200(message: Localizer["processingQueue.processed"]);
        }

        [RequireAccess]
        [DeleteEndpoint("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            ProcessingQueueContract? contract = await QueryContracts()
                .FirstOrDefaultAsync(current => current.Id == id, cancellationToken);

            ProcessingQueue? queue = await processingQueueService.Delete(id, cancellationToken);
            if (queue is null)
            {
                return Http404(processingQueueService.GetErrorMessages());
            }

            return Http200(contract ?? ProcessingQueueContract.Projection.Compile()(queue), Localizer["processingQueue.deleted"]);
        }
    }
}
