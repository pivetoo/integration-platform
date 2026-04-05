using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using IntegrationPlataform.Api.Contracts.Execution;
using IntegrationPlataform.Application.Localization;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace IntegrationPlataform.Api.Controllers
{
    public sealed class ExecutionLogsController : ApiControllerBase
    {
        private readonly IExecutionLogService executionLogService;
        private static readonly Func<ExecutionLog, ExecutionLogContract> MapExecutionLog = ExecutionLogContract.Projection.Compile();
        private new IStringLocalizer<IntegrationPlataformResource> Localizer { get; }

        public ExecutionLogsController(IExecutionLogService executionLogService, IStringLocalizer<IntegrationPlataformResource> localizer)
        {
            this.executionLogService = executionLogService;
            Localizer = localizer;
        }

        [RequireAccess]
        [GetEndpoint]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            PagedResult<ExecutionLog> result = await executionLogService.GetExecutionLogs(request, cancellationToken);
            return Http200(new PagedResult<ExecutionLogContract>
            {
                Items = result.Items.Select(MapExecutionLog).ToArray(),
                Pagination = result.Pagination
            });
        }

        [RequireAccess]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            ExecutionLog? log = await executionLogService.GetExecutionLogById(id, cancellationToken);

            return log is null ? Http404(Localizer["execution.log.notFound"]) : Http200(MapExecutionLog(log));
        }

        [RequireAccess]
        [GetEndpoint("execution/{executionId:long}")]
        public async Task<IActionResult> GetByExecution(long executionId, CancellationToken cancellationToken)
        {
            if (executionId <= 0)
            {
                return Http400(Localizer["request.execution.id.required"]);
            }

            List<ExecutionLog> logs = await executionLogService.GetExecutionLogsByExecution(executionId, cancellationToken);

            return Http200(logs.Select(MapExecutionLog).ToList());
        }

        [RequireAccess]
        [DeleteEndpoint("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            ExecutionLog? log = await executionLogService.Delete(id, cancellationToken);
            if (log is null)
            {
                return Http404(executionLogService.GetErrorMessages());
            }

            return Http200(MapExecutionLog(log), Localizer["execution.log.deleted"]);
        }
    }
}
