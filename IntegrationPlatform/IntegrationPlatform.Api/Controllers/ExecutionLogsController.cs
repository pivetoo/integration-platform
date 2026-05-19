using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using IntegrationPlatform.Api.Contracts.Execution;
using IntegrationPlatform.Application.Localization;
using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace IntegrationPlatform.Api.Controllers
{
    public sealed class ExecutionLogsController : ApiControllerBase
    {
        private readonly IExecutionLogService executionLogService;
        private static readonly Func<ExecutionLog, ExecutionLogContract> MapExecutionLog = ExecutionLogContract.Projection.Compile();
        private new IStringLocalizer<IntegrationPlatformResource> Localizer { get; }

        public ExecutionLogsController(IExecutionLogService executionLogService, IStringLocalizer<IntegrationPlatformResource> localizer)
        {
            this.executionLogService = executionLogService;
            Localizer = localizer;
        }

        [RequireAccess("Permite listar os logs de execução registrados na plataforma.")]
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

        [RequireAccess("Permite consultar os detalhes de um log de execução específico.")]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            ExecutionLog? log = await executionLogService.GetExecutionLogById(id, cancellationToken);

            return log is null ? Http404(Localizer["execution.log.notFound"]) : Http200(MapExecutionLog(log));
        }

        [RequireAccess("Permite listar os logs vinculados a uma execução específica.")]
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

        [RequireAccess("Permite excluir um log de execução registrado.")]
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
