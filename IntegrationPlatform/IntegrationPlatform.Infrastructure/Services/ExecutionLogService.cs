using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using IntegrationPlatform.Application.Localization;
using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace IntegrationPlatform.Infrastructure.Services
{
    public sealed class ExecutionLogService : CrudService<ExecutionLog>, IExecutionLogService
    {
        private readonly IStringLocalizer<IntegrationPlatformResource> Localizer;

        public ExecutionLogService(DbContext dbContext, IStringLocalizer<IntegrationPlatformResource> localizer) : base(dbContext)
        {
            Localizer = localizer;
        }

        public async Task<PagedResult<ExecutionLog>> GetExecutionLogs(PagedRequest request, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<ExecutionLog>()
                .AsNoTracking()
                .Include(item => item.PipelineStep)
                .OrderByDescending(item => item.CreatedAt)
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<ExecutionLog?> GetExecutionLogById(long id, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<ExecutionLog>()
                .AsNoTracking()
                .Include(item => item.PipelineStep)
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<List<ExecutionLog>> GetExecutionLogsByExecution(long executionId, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<ExecutionLog>()
                .AsNoTracking()
                .Include(item => item.PipelineStep)
                .Where(item => item.ExecutionId == executionId)
                .OrderBy(item => item.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public override async Task<ExecutionLog?> Delete(long id, CancellationToken cancellationToken = default)
        {
            MutableMessages.Clear();

            ExecutionLog? log = await DbContext.Set<ExecutionLog>()
                .AsNoTracking()
                .FirstOrDefaultAsync(current => current.Id == id, cancellationToken);

            if (log is null)
            {
                MutableMessages.Add(new KeyNotFoundException(Localizer["execution.log.notFound"]));
                return null;
            }

            return await Delete([log], cancellationToken) ? log : null;
        }
    }
}
