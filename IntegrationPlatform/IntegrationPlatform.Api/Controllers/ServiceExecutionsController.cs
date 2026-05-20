using Archon.Api.Attributes;
using Archon.Api.Controllers;
using IntegrationPlatform.Api.Contracts.Execution;
using IntegrationPlatform.Application.Localization;
using IntegrationPlatform.Application.Requests.ServiceContracts;
using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace IntegrationPlatform.Api.Controllers
{
    [Route("api/services")]
    [AccessArea("serviceExecutions.area")]
    public sealed class ServiceExecutionsController : ApiControllerBase
    {
        private readonly IServiceExecutionService serviceExecutionService;
        private new IStringLocalizer<IntegrationPlatformResource> Localizer { get; }

        public ServiceExecutionsController(IServiceExecutionService serviceExecutionService, IStringLocalizer<IntegrationPlatformResource> localizer)
        {
            this.serviceExecutionService = serviceExecutionService;
            Localizer = localizer;
        }

        [RequireAccess("serviceExecutions.execute.description")]
        [HttpPost("{identifier}/execute")]
        public async Task<IActionResult> Execute(string identifier, [FromBody] ExecuteServiceRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            if (string.IsNullOrWhiteSpace(identifier))
            {
                return Http400(Localizer["serviceContract.identifier.required"]);
            }

            Execution execution = await serviceExecutionService.ExecuteService(identifier, request.ConnectorId, request.InputData, cancellationToken);
            return Http200(DebugPipelineResultContract.FromExecution(execution));
        }

        [RequireAccess("serviceExecutions.enqueue.description")]
        [HttpPost("{identifier}/enqueue")]
        public async Task<IActionResult> Enqueue(string identifier, [FromBody] EnqueueServiceRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            if (string.IsNullOrWhiteSpace(identifier))
            {
                return Http400(Localizer["serviceContract.identifier.required"]);
            }

            ProcessingQueue queue = await serviceExecutionService.EnqueueService(identifier, request.ConnectorId, request.InputData, request.Priority, request.ScheduledFor, cancellationToken);
            return Http200(queue, Localizer["serviceExecution.enqueued"]);
        }
    }
}
