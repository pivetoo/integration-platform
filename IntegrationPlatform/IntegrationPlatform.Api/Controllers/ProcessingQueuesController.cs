using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using IntegrationPlatform.Api.Contracts.ProcessingQueues;
using IntegrationPlatform.Application.Localization;
using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace IntegrationPlatform.Api.Controllers
{
    public sealed class ProcessingQueuesController : ApiControllerBase
    {
        private readonly IExecutionService executionService;
        private readonly IProcessingQueueService processingQueueService;
        private readonly IQueueProcessorService queueProcessorService;
        private new IStringLocalizer<IntegrationPlatformResource> Localizer { get; }
        private static readonly Func<ProcessingQueue, ProcessingQueueContract> MapProcessingQueue = ProcessingQueueContract.Projection.Compile();

        public ProcessingQueuesController(
            IExecutionService executionService,
            IProcessingQueueService processingQueueService,
            IQueueProcessorService queueProcessorService,
            IStringLocalizer<IntegrationPlatformResource> localizer)
        {
            this.executionService = executionService;
            this.processingQueueService = processingQueueService;
            this.queueProcessorService = queueProcessorService;
            Localizer = localizer;
        }

        [RequireAccess("Permite listar os itens da fila de processamento da plataforma.")]
        [GetEndpoint("[action]")]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, [FromQuery] string? search, CancellationToken cancellationToken)
        {
            PagedResult<ProcessingQueue> result = await processingQueueService.GetProcessingQueues(request, search, cancellationToken);
            return Http200(new PagedResult<ProcessingQueueContract>
            {
                Items = result.Items.Select(MapProcessingQueue).ToArray(),
                Pagination = result.Pagination
            });
        }

        [RequireAccess("Permite consultar os detalhes de um item da fila de processamento.")]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            ProcessingQueue? item = await processingQueueService.GetProcessingQueueById(id, cancellationToken);

            return item is null ? Http404(Localizer["processingQueue.notFound"]) : Http200(MapProcessingQueue(item));
        }

        [RequireAccess("Permite listar os itens pendentes da fila de processamento.")]
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

        [RequireAccess("Permite processar manualmente um item específico da fila.")]
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

        [RequireAccess("Permite excluir um item da fila de processamento.")]
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
