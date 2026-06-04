using IntegrationPlatform.Domain.Entities;
using IntegrationPlatform.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IntegrationPlatform.Infrastructure.BackgroundServices
{
    public sealed class PipelineRoutineSchedulerService : BackgroundService
    {
        private readonly TenantJobRunner tenantJobRunner;
        private readonly ILogger<PipelineRoutineSchedulerService> logger;
        private readonly BackgroundJobOptions options;

        public PipelineRoutineSchedulerService(
            TenantJobRunner tenantJobRunner,
            ILogger<PipelineRoutineSchedulerService> logger,
            IOptions<BackgroundJobOptions> options)
        {
            this.tenantJobRunner = tenantJobRunner;
            this.logger = logger;
            this.options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!options.SchedulerEnabled)
            {
                logger.LogInformation("PipelineRoutine scheduler is disabled.");
                return;
            }

            logger.LogInformation("PipelineRoutine scheduler started. Polling every {Interval}.", options.SchedulerPollingInterval);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ScheduleDueRoutines(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Unexpected error while scheduling pipeline routines.");
                }

                try
                {
                    await Task.Delay(options.SchedulerPollingInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            logger.LogInformation("PipelineRoutine scheduler stopped.");
        }

        private async Task ScheduleDueRoutines(CancellationToken cancellationToken)
        {
            await tenantJobRunner.RunForAllTenants(async (provider, tenantCancellationToken) =>
            {
                DbContext dbContext = provider.GetRequiredService<DbContext>();

                DateTimeOffset now = DateTimeOffset.UtcNow;

                List<PipelineRoutine> dueRoutines = await dbContext.Set<PipelineRoutine>()
                    .AsTracking()
                    .Where(routine => routine.IsActive
                        && (routine.NextExecutionAt == null || routine.NextExecutionAt <= now))
                    .OrderBy(routine => routine.NextExecutionAt ?? DateTimeOffset.MinValue)
                    .Take(options.MaxItemsPerCycle)
                    .ToListAsync(tenantCancellationToken);

                if (dueRoutines.Count == 0)
                {
                    return;
                }

                logger.LogInformation("Found {Count} pipeline routine(s) due for execution.", dueRoutines.Count);

                foreach (PipelineRoutine routine in dueRoutines)
                {
                    tenantCancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        ProcessingQueue queueItem = new(
                            routine.ConnectorId,
                            routine.PipelineId,
                            priority: 0,
                            status: ProcessingStatus.Pending,
                            payload: routine.DefaultPayload,
                            scheduledAt: now);

                        queueItem.SetCreatedAt(now);
                        dbContext.Set<ProcessingQueue>().Add(queueItem);

                        DateTimeOffset nextExecution = now.AddMinutes(routine.IntervalInMinutes);
                        routine.MarkExecution(now, nextExecution);
                        routine.SetUpdatedAt(now);

                        await dbContext.SaveChangesAsync(tenantCancellationToken);

                        logger.LogInformation(
                            "Enqueued routine {RoutineId} (connector {ConnectorId}, pipeline {PipelineId}). Next execution at {NextExecution}.",
                            routine.Id,
                            routine.ConnectorId,
                            routine.PipelineId,
                            nextExecution);
                    }
                    catch (Exception exception)
                    {
                        logger.LogError(exception, "Failed to enqueue routine {RoutineId}.", routine.Id);
                    }
                }
            }, cancellationToken);
        }
    }
}
