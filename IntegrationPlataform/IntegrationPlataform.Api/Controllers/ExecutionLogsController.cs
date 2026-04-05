using Archon.Api.Attributes;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using IntegrationPlataform.Api.Contracts.Execution;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Api.Controllers
{
    public sealed class ExecutionLogsController : IntegrationPlataformReadOnlyController<ExecutionLog>
    {
        private readonly IExecutionLogService executionLogService;

        public ExecutionLogsController(DbContext dbContext, IExecutionLogService executionLogService) : base(dbContext)
        {
            this.executionLogService = executionLogService;
        }

        private IQueryable<ExecutionLogContract> QueryContracts()
        {
            return DbContext.Set<ExecutionLog>()
                .AsNoTracking()
                .Select(ExecutionLogContract.Projection);
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
            ExecutionLogContract? log = await QueryContracts()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            return log is null ? Http404(Localizer["execution.log.notFound"]) : Http200(log);
        }

        [RequireAccess]
        [GetEndpoint("execution/{executionId:long}")]
        public async Task<IActionResult> GetByExecution(long executionId, CancellationToken cancellationToken)
        {
            if (executionId <= 0)
            {
                return Http400(Localizer["request.execution.id.required"]);
            }

            List<ExecutionLogContract> logs = await QueryContracts()
                .Where(item => item.ExecutionId == executionId)
                .OrderBy(item => item.CreatedAt)
                .ToListAsync(cancellationToken);

            return Http200(logs);
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

            return Http200(log, Localizer["execution.log.deleted"]);
        }
    }
}
