using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using IntegrationPlatform.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlatform.Infrastructure.Services.ExecutionEngine
{
    public sealed class QueueProcessorService : IQueueProcessorService
    {
        private readonly DbContext dbContext;
        private readonly IExecutionEngineService executionEngineService;

        public QueueProcessorService(DbContext dbContext, IExecutionEngineService executionEngineService)
        {
            this.dbContext = dbContext;
            this.executionEngineService = executionEngineService;
        }

        public async Task<IReadOnlyCollection<ProcessingQueue>> GetPendingToProcess(CancellationToken cancellationToken = default)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            List<ProcessingQueue> items = await (
                from item in dbContext.Set<ProcessingQueue>().AsNoTracking()
                where item.Status == ProcessingStatus.Pending &&
                      (!item.ScheduledAt.HasValue || item.ScheduledAt <= now)
                orderby item.Priority, item.CreatedAt
                select item)
                .ToListAsync(cancellationToken);

            return items;
        }

        public async Task ProcessItem(long processingQueueId, CancellationToken cancellationToken = default)
        {
            ProcessingQueue? item = await dbContext.Set<ProcessingQueue>()
                .AsTracking()
                .FirstOrDefaultAsync(current => current.Id == processingQueueId, cancellationToken);

            if (item is null || item.Status != ProcessingStatus.Pending)
            {
                return;
            }

            try
            {
                item.MarkAsProcessing(DateTimeOffset.UtcNow);
                item.SetUpdatedAt(DateTimeOffset.UtcNow);
                await dbContext.SaveChangesAsync(cancellationToken);

                await executionEngineService.ExecutePipeline(
                    item.ConnectorId,
                    item.PipelineId,
                    item.Payload,
                    ExecutionType.Pipeline,
                    item,
                    null,
                    cancellationToken);
            }
            catch (Exception exception)
            {
                item.Fail(exception.Message, DateTimeOffset.UtcNow);
                item.SetUpdatedAt(DateTimeOffset.UtcNow);
                await dbContext.SaveChangesAsync(cancellationToken);
                throw;
            }
        }
    }
}
