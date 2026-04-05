using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using IntegrationPlataform.Api.Contracts.ProcessingQueues;
using IntegrationPlataform.Application.Localization;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace IntegrationPlataform.Api.Controllers
{
    public sealed class ProcessingQueuesController : ApiControllerBase
    {
        private readonly IExecutionService executionService;
        private readonly IProcessingQueueService processingQueueService;
        private readonly IQueueProcessorService queueProcessorService;
        private new IStringLocalizer<IntegrationPlataformResource> Localizer { get; }
        private static readonly Func<ProcessingQueue, ProcessingQueueContract> MapProcessingQueue = ProcessingQueueContract.Projection.Compile();

        public ProcessingQueuesController(
            IExecutionService executionService,
            IProcessingQueueService processingQueueService,
            IQueueProcessorService queueProcessorService,
            IStringLocalizer<IntegrationPlataformResource> localizer)
        {
            this.executionService = executionService;
            this.processingQueueService = processingQueueService;
            this.queueProcessorService = queueProcessorService;
            Localizer = localizer;
        }

        [RequireAccess]
        [GetEndpoint]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            PagedResult<ProcessingQueue> result = await processingQueueService.GetProcessingQueues(request, cancellationToken);
            return Http200(new PagedResult<ProcessingQueueContract>
            {
                Items = result.Items.Select(MapProcessingQueue).ToArray(),
                Pagination = result.Pagination
            });
        }

        [RequireAccess]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            ProcessingQueue? item = await processingQueueService.GetProcessingQueueById(id, cancellationToken);

            return item is null ? Http404(Localizer["processingQueue.notFound"]) : Http200(MapProcessingQueue(item));
        }

        [RequireAccess]
        [GetEndpoint("pending")]
        public async Task<IActionResult> GetPending(CancellationToken cancellationToken)
        {
            List<ProcessingQueue> items = await processingQueueService.GetPendingProcessingQueues(cancellationToken);

            return Http200(items.Select(MapProcessingQueue).ToList());
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
            ProcessingQueue? queue = await processingQueueService.GetProcessingQueueById(item.Id, cancellationToken);
            return Http201(MapProcessingQueue(queue ?? item), Localizer["processingQueue.enqueued"]);
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
            ProcessingQueue? queue = await processingQueueService.Delete(id, cancellationToken);
            if (queue is null)
            {
                return Http404(processingQueueService.GetErrorMessages());
            }

            return Http200(MapProcessingQueue(queue), Localizer["processingQueue.deleted"]);
        }
    }
}
