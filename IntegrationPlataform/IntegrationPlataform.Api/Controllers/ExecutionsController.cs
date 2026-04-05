using Archon.Api.Attributes;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using IntegrationPlataform.Api.Contracts.Execution;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using IntegrationPlataform.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Api.Controllers
{
    public sealed class ExecutionsController : IntegrationPlataformControllerBase
    {
        private readonly DbContext dbContext;
        private readonly IExecutionEngineService executionEngineService;

        public ExecutionsController(DbContext dbContext, IExecutionEngineService executionEngineService)
        {
            this.dbContext = dbContext;
            this.executionEngineService = executionEngineService;
        }

        private IQueryable<ExecutionContract> QueryContracts()
        {
            return dbContext.Set<Execution>()
                .AsNoTracking()
                .Select(ExecutionContract.Projection);
        }

        [RequireAccess]
        [GetEndpoint]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            var result = await QueryContracts()
                .OrderByDescending(item => item.StartedAt)
                .ToPagedResultAsync(request, cancellationToken);

            return Http200(result);
        }

        [RequireAccess]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return Http400(Localizer["request.execution.id.required"]);
            }

            ExecutionContract? execution = await QueryContracts()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            return execution is null ? Http404(Localizer["execution.notFound"]) : Http200(execution);
        }

        [RequireAccess]
        [GetEndpoint("connector/{connectorId:long}")]
        public async Task<IActionResult> GetByConnector(long connectorId, CancellationToken cancellationToken)
        {
            if (connectorId <= 0)
            {
                return Http400(Localizer["request.connector.id.required"]);
            }

            var executions = await QueryContracts()
                .Where(item => item.ConnectorId == connectorId)
                .OrderByDescending(item => item.StartedAt)
                .ToListAsync(cancellationToken);

            return Http200(executions);
        }

        [RequireAccess]
        [GetEndpoint("status/{status}")]
        public async Task<IActionResult> GetByStatus(ExecutionStatus status, CancellationToken cancellationToken)
        {
            var executions = await QueryContracts()
                .Where(item => item.Status == status)
                .OrderByDescending(item => item.StartedAt)
                .ToListAsync(cancellationToken);

            return Http200(executions);
        }

        [RequireAccess]
        [GetEndpoint("recent")]
        public async Task<IActionResult> GetRecent([FromQuery] int take = 10, CancellationToken cancellationToken = default)
        {
            int normalizedTake = take <= 0 ? 10 : take;

            var executions = await QueryContracts()
                .OrderByDescending(item => item.StartedAt)
                .Take(normalizedTake)
                .ToListAsync(cancellationToken);

            return Http200(executions);
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

        [RequireAccess]
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

        [RequireAccess]
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

        [RequireAccess]
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

        [RequireAccess]
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

        [RequireAccess]
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
