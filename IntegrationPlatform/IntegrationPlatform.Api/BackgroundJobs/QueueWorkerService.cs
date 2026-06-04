using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using IntegrationPlatform.Infrastructure.BackgroundJobs;
using Microsoft.Extensions.Options;

namespace IntegrationPlatform.Api.BackgroundJobs
{
    public sealed class QueueWorkerService : BackgroundService
    {
        private readonly TenantJobRunner tenantJobRunner;
        private readonly ILogger<QueueWorkerService> logger;
        private readonly BackgroundJobOptions options;

        public QueueWorkerService(
            TenantJobRunner tenantJobRunner,
            ILogger<QueueWorkerService> logger,
            IOptions<BackgroundJobOptions> options)
        {
            this.tenantJobRunner = tenantJobRunner;
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
            int processedCount = 0;

            await tenantJobRunner.RunForAllTenants(async (provider, tenantCancellationToken) =>
            {
                IQueueProcessorService queueProcessorService = provider.GetRequiredService<IQueueProcessorService>();
                IReadOnlyCollection<ProcessingQueue> pending = await queueProcessorService.GetPendingToProcess(tenantCancellationToken);
                List<long> pendingIds = pending.Take(options.MaxItemsPerCycle).Select(item => item.Id).ToList();

                foreach (long id in pendingIds)
                {
                    tenantCancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        await queueProcessorService.ProcessItem(id, tenantCancellationToken);
                        processedCount++;
                    }
                    catch (Exception exception)
                    {
                        logger.LogError(exception, "Failed to process queue item {QueueItemId}.", id);
                    }
                }
            }, cancellationToken);

            return processedCount;
        }
    }
}
