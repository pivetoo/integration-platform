using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using IntegrationPlatform.Api.Contracts.Execution;
using IntegrationPlatform.Application.Localization;
using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using IntegrationPlatform.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace IntegrationPlatform.Api.Controllers
{
    [AccessArea("executions.area")]
    public sealed class ExecutionsController : ApiControllerBase
    {
        private readonly IExecutionService executionService;
        private readonly IExecutionEngineService executionEngineService;
        private new IStringLocalizer<IntegrationPlatformResource> Localizer { get; }
        private static readonly Func<Execution, ExecutionContract> MapExecution = ExecutionContract.Projection.Compile();

        public ExecutionsController(IExecutionService executionService, IExecutionEngineService executionEngineService, IStringLocalizer<IntegrationPlatformResource> localizer)
        {
            this.executionService = executionService;
            this.executionEngineService = executionEngineService;
            Localizer = localizer;
        }

        [RequireAccess("executions.get.description")]
        [GetEndpoint]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, [FromQuery] string? search, CancellationToken cancellationToken)
        {
            PagedResult<Execution> result = await executionService.GetExecutions(request, search, cancellationToken);
            return Http200(new PagedResult<ExecutionContract>
            {
                Items = result.Items.Select(MapExecution).ToArray(),
                Pagination = result.Pagination
            });
        }

        [RequireAccess("executions.getById.description")]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return Http400(Localizer["request.execution.id.required"]);
            }

            Execution? execution = await executionService.GetExecutionById(id, cancellationToken);

            return execution is null ? Http404(Localizer["execution.notFound"]) : Http200(MapExecution(execution));
        }

        [RequireAccess("executions.getByConnector.description")]
        [GetEndpoint("connector/{connectorId:long}")]
        public async Task<IActionResult> GetByConnector(long connectorId, CancellationToken cancellationToken)
        {
            if (connectorId <= 0)
            {
                return Http400(Localizer["request.connector.id.required"]);
            }

            IReadOnlyCollection<Execution> executions = await executionService.GetByConnector(connectorId, cancellationToken);

            return Http200(executions.Select(MapExecution).ToList());
        }

        [RequireAccess("executions.getByStatus.description")]
        [GetEndpoint("status/{status}")]
        public async Task<IActionResult> GetByStatus(ExecutionStatus status, CancellationToken cancellationToken)
        {
            IReadOnlyCollection<Execution> executions = await executionService.GetByStatus(status, cancellationToken);

            return Http200(executions.Select(MapExecution).ToList());
        }

        [RequireAccess("executions.getRecent.description")]
        [GetEndpoint("recent")]
        public async Task<IActionResult> GetRecent([FromQuery] int take = 10, CancellationToken cancellationToken = default)
        {
            int normalizedTake = take <= 0 ? 10 : take;

            IReadOnlyCollection<Execution> executions = await executionService.GetRecent(normalizedTake, cancellationToken);

            return Http200(executions.Select(MapExecution).ToList());
        }

        [RequireAccess]
        [PostEndpoint("execute")]
        public async Task<IActionResult> Execute([FromBody] ExecutePipelineRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            Execution execution = await executionEngineService.ExecutePipeline(
                request.ConnectorId,
                request.PipelineId,
                request.InputData,
                ExecutionType.Manual,
                cancellationToken: cancellationToken);

            return Http200(DebugPipelineResultContract.FromExecution(execution));
        }

        [RequireAccess("executions.debug.description")]
        [PostEndpoint("debug")]
        public async Task<IActionResult> Debug([FromBody] DebugPipelineRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            Execution execution = await executionEngineService.ExecutePipeline(
                request.ConnectorId,
                request.PipelineId,
                request.InputData,
                ExecutionType.Manual,
                initialStepId: request.InitialStepId,
                cancellationToken: cancellationToken);

            return Http200(DebugPipelineResultContract.FromExecution(execution));
        }

        [RequireAccess("executions.startDebug.description")]
        [PostEndpoint("debug/start")]
        public async Task<IActionResult> StartDebug([FromBody] DebugPipelineRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var session = await executionEngineService.StartDebugPipeline(
                request.ConnectorId,
                request.PipelineId,
                request.InputData,
                request.InitialStepId,
                cancellationToken);

            int remainingSteps = session.ActiveSteps.Count - session.CurrentIndex;
            PipelineStep? nextStep = remainingSteps > 0 ? session.ActiveSteps[session.CurrentIndex] : null;

            return Http200(new StartDebugPipelineResultContract
            {
                DebugSessionId = session.SessionId,
                ExecutionId = session.Execution.Id,
                Status = session.Execution.Status,
                TotalSteps = session.ActiveSteps.Count,
                RemainingSteps = remainingSteps,
                NextStepId = nextStep?.Id,
                NextStepName = nextStep?.Name
            });
        }

        [RequireAccess("executions.executeNextDebugStep.description")]
        [PostEndpoint("debug/next-step")]
        public async Task<IActionResult> ExecuteNextDebugStep([FromBody] NextDebugStepRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var result = await executionEngineService.ExecuteNextDebugStep(request.DebugSessionId, cancellationToken);
            return Http200(ExecuteNextDebugStepResultContract.FromModel(result));
        }

        [RequireAccess("executions.finishDebug.description")]
        [PostEndpoint("debug/finish")]
        public async Task<IActionResult> FinishDebug([FromBody] FinishDebugPipelineRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            Execution execution = await executionEngineService.FinishDebugPipeline(request.DebugSessionId, cancellationToken);
            return Http200(DebugPipelineResultContract.FromExecution(execution));
        }

        [RequireAccess("executions.executeByIdentifier.description")]
        [PostEndpoint("execute-by-identifier/{integrationIdentifier}/{pipelineIdentifier}")]
        public async Task<IActionResult> ExecuteByIdentifier(
            string integrationIdentifier,
            string pipelineIdentifier,
            [FromBody] ExecutePipelineByIdentifierRequest? request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(integrationIdentifier))
            {
                return Http400(Localizer["request.integration.identifier.required"]);
            }

            if (string.IsNullOrWhiteSpace(pipelineIdentifier))
            {
                return Http400(Localizer["request.pipeline.identifier.required"]);
            }

            Execution execution = await executionEngineService.ExecutePipelineByIdentifier(
                integrationIdentifier,
                pipelineIdentifier,
                request?.InputData ?? [],
                ExecutionType.Manual,
                cancellationToken);

            return Http200(DebugPipelineResultContract.FromExecution(execution));
        }
    }
}
