using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IntegrationPlatform.Infrastructure.BackgroundServices
{
    public sealed class QueueWorkerService : BackgroundService
    {
        private readonly IServiceScopeFactory scopeFactory;
        private readonly ILogger<QueueWorkerService> logger;
        private readonly BackgroundJobOptions options;

        public QueueWorkerService(
            IServiceScopeFactory scopeFactory,
            ILogger<QueueWorkerService> logger,
            IOptions<BackgroundJobOptions> options)
        {
            this.scopeFactory = scopeFactory;
            this.logger = logger;
            this.options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!options.QueueWorkerEnabled)
            {
                logger.LogInformation("Processing queue worker is disabled.");
                return;
            }

            logger.LogInformation("Processing queue worker started. Polling every {Interval}.", options.QueueWorkerPollingInterval);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    int processed = await ProcessPendingItems(stoppingToken);
                    if (processed > 0)
                    {
                        logger.LogInformation("Processed {Count} item(s) from the queue.", processed);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Unexpected error while processing the queue.");
                }

                try
                {
                    await Task.Delay(options.QueueWorkerPollingInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            logger.LogInformation("Processing queue worker stopped.");
        }

        private async Task<int> ProcessPendingItems(CancellationToken cancellationToken)
        {
            List<long> pendingIds;

            using (IServiceScope scope = scopeFactory.CreateScope())
            {
                IQueueProcessorService queueProcessorService = scope.ServiceProvider.GetRequiredService<IQueueProcessorService>();
                IReadOnlyCollection<ProcessingQueue> pending = await queueProcessorService.GetPendingToProcess(cancellationToken);
                pendingIds = pending.Take(options.MaxItemsPerCycle).Select(item => item.Id).ToList();
            }

            if (pendingIds.Count == 0)
            {
                return 0;
            }

            int processedCount = 0;

            foreach (long id in pendingIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    using IServiceScope scope = scopeFactory.CreateScope();
                    IQueueProcessorService queueProcessorService = scope.ServiceProvider.GetRequiredService<IQueueProcessorService>();

                    await queueProcessorService.ProcessItem(id, cancellationToken);
                    processedCount++;
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Failed to process queue item {QueueItemId}.", id);
                }
            }

            return processedCount;
        }
    }
}
