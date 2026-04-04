using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Api.Controllers
{
    public sealed class ExecutionLogsController : ReadOnlyController<ExecutionLog>
    {
        private readonly IExecutionLogService executionLogService;

        public ExecutionLogsController(DbContext dbContext, IExecutionLogService executionLogService) : base(dbContext)
        {
            this.executionLogService = executionLogService;
        }

        [RequireAccess]
        [GetEndpoint]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            return await base.Get(request, cancellationToken);
        }

        [RequireAccess]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            return await base.GetById(id, cancellationToken);
        }

        [RequireAccess]
        [GetEndpoint("execution/{executionId:long}")]
        public async Task<IActionResult> GetByExecution(long executionId, CancellationToken cancellationToken)
        {
            if (executionId <= 0)
            {
                return Http400("Execution id is required.");
            }

            List<ExecutionLog> logs = await DbContext.Set<ExecutionLog>()
                .AsNoTracking()
                .Where(item => item.ExecutionId == executionId)
                .OrderBy(item => item.Id)
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

            return Http200(log, "Execution log deleted successfully.");
        }
    }
}
