using Archon.Api.Attributes;
using Archon.Api.Controllers;
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
    public sealed class ExecutionsController : ApiControllerBase
    {
        private readonly DbContext dbContext;
        private readonly IExecutionService executionService;
        private readonly IExecutionEngineService executionEngineService;

        public ExecutionsController(DbContext dbContext, IExecutionService executionService, IExecutionEngineService executionEngineService)
        {
            this.dbContext = dbContext;
            this.executionService = executionService;
            this.executionEngineService = executionEngineService;
        }

        [RequireAccess]
        [GetEndpoint]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            var result = await dbContext.Set<Execution>()
                .AsNoTracking()
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
                return Http400("Id is required.");
            }

            Execution? execution = await dbContext.Set<Execution>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            return execution is null ? Http404("Record not found.") : Http200(execution);
        }

        [RequireAccess]
        [GetEndpoint("connector/{connectorId:long}")]
        public async Task<IActionResult> GetByConnector(long connectorId, CancellationToken cancellationToken)
        {
            if (connectorId <= 0)
            {
                return Http400("Connector id is required.");
            }

            var executions = await executionService.GetByConnector(connectorId, cancellationToken);
            return Http200(executions);
        }

        [RequireAccess]
        [GetEndpoint("status/{status}")]
        public async Task<IActionResult> GetByStatus(ExecutionStatus status, CancellationToken cancellationToken)
        {
            var executions = await executionService.GetByStatus(status, cancellationToken);
            return Http200(executions);
        }

        [RequireAccess]
        [GetEndpoint("recent")]
        public async Task<IActionResult> GetRecent([FromQuery] int take = 10, CancellationToken cancellationToken = default)
        {
            var executions = await executionService.GetRecent(take, cancellationToken);
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

            return Http200(CreateExecutionResponse(execution));
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

            return Http200(new
            {
                session.SessionId,
                ExecutionId = session.Execution.Id,
                PipelineId = session.Pipeline.Id,
                ConnectorId = session.Connector.Id,
                TotalSteps = session.ActiveSteps.Count
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
            return Http200(result);
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
            return Http200(CreateExecutionResponse(execution));
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
                return Http400("Integration identifier is required.");
            }

            if (string.IsNullOrWhiteSpace(pipelineIdentifier))
            {
                return Http400("Pipeline identifier is required.");
            }

            Execution execution = await executionEngineService.ExecutePipelineByIdentifier(
                integrationIdentifier,
                pipelineIdentifier,
                request?.InputData ?? [],
                ExecutionType.Manual,
                cancellationToken);

            return Http200(CreateExecutionResponse(execution));
        }

        private static object CreateExecutionResponse(Execution execution)
        {
            return new
            {
                execution.Id,
                execution.Status,
                execution.Type,
                execution.StartedAt,
                execution.FinishedAt,
                execution.Duration,
                execution.OutputData,
                execution.Errors
            };
        }
    }
}
