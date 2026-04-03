using Archon.Infrastructure.Services;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using IntegrationPlataform.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Infrastructure.Services
{
    public sealed class ExecutionService : CrudService<Execution>, IExecutionService
    {
        public ExecutionService(DbContext dbContext) : base(dbContext)
        {
        }

        public async Task<IReadOnlyCollection<Execution>> GetByConnector(long connectorId, CancellationToken cancellationToken = default)
        {
            List<Execution> executions = await (
                from execution in DbContext.Set<Execution>().AsNoTracking()
                where execution.ConnectorId == connectorId
                orderby execution.StartedAt descending
                select execution)
                .ToListAsync(cancellationToken);

            return executions;
        }

        public async Task<IReadOnlyCollection<Execution>> GetByStatus(ExecutionStatus status, CancellationToken cancellationToken = default)
        {
            List<Execution> executions = await (
                from execution in DbContext.Set<Execution>().AsNoTracking()
                where execution.Status == status
                orderby execution.StartedAt descending
                select execution)
                .ToListAsync(cancellationToken);

            return executions;
        }

        public async Task<IReadOnlyCollection<Execution>> GetRecent(int take, CancellationToken cancellationToken = default)
        {
            int normalizedTake = take <= 0 ? 10 : take;

            List<Execution> executions = await DbContext.Set<Execution>()
                .AsNoTracking()
                .Include(item => item.Connector)
                .Include(item => item.Pipeline)
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
    }
}
