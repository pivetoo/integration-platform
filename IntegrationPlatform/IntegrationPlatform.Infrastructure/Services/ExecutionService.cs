using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using IntegrationPlatform.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlatform.Infrastructure.Services
{
    public sealed class ExecutionService : CrudService<Execution>, IExecutionService
    {
        public ExecutionService(DbContext dbContext) : base(dbContext)
        {
        }

        public async Task<PagedResult<Execution>> GetExecutions(PagedRequest request, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .OrderByDescending(item => item.StartedAt)
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<Execution?> GetExecutionById(long id, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyCollection<Execution>> GetByConnector(long connectorId, CancellationToken cancellationToken = default)
        {
            List<Execution> executions = await QueryWithDetails()
                .Where(execution => execution.ConnectorId == connectorId)
                .OrderByDescending(execution => execution.StartedAt)
                .ToListAsync(cancellationToken);

            return executions;
        }

        public async Task<IReadOnlyCollection<Execution>> GetByStatus(ExecutionStatus status, CancellationToken cancellationToken = default)
        {
            List<Execution> executions = await QueryWithDetails()
                .Where(execution => execution.Status == status)
                .OrderByDescending(execution => execution.StartedAt)
                .ToListAsync(cancellationToken);

            return executions;
        }

        public async Task<IReadOnlyCollection<Execution>> GetRecent(int take, CancellationToken cancellationToken = default)
        {
            int normalizedTake = take <= 0 ? 10 : take;

            List<Execution> executions = await QueryWithDetails()
                .OrderByDescending(item => item.StartedAt)
                .Take(normalizedTake)
                .ToListAsync(cancellationToken);

            return executions;
        }

        public async Task<ProcessingQueue> EnqueuePipeline(long connectorId, long pipelineId, string? payload, int priority, CancellationToken cancellationToken = default)
        {
            ProcessingQueue queueItem = new(
                connectorId,
                pipelineId,
                priority,
                ProcessingStatus.Pending,
                payload);

            queueItem.SetCreatedAt(DateTimeOffset.UtcNow);
            DbContext.Set<ProcessingQueue>().Add(queueItem);
            await DbContext.SaveChangesAsync(cancellationToken);

            return queueItem;
        }

        private IQueryable<Execution> QueryWithDetails()
        {
            return DbContext.Set<Execution>()
                .AsNoTracking()
                .Include(item => item.Connector)
                .ThenInclude(item => item!.Integration)
                .ThenInclude(item => item!.IntegrationCategory)
                .Include(item => item.Pipeline)
                .ThenInclude(item => item!.Integration)
                .ThenInclude(item => item!.IntegrationCategory);
        }
    }
}
