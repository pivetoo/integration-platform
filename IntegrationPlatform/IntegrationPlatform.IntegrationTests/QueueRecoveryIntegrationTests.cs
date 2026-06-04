using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using IntegrationPlatform.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationPlatform.IntegrationTests
{
    [TestFixture]
    public sealed class QueueRecoveryIntegrationTests : IntegrationTestBase
    {
        [Test]
        public async Task RecoverStuckItems_marks_processing_items_past_timeout_as_error()
        {
            await InScopeAsync(async serviceProvider =>
            {
                (long connectorId, long pipelineId) = await SeedConnectorAndPipeline(serviceProvider);
                DbContext dbContext = serviceProvider.GetRequiredService<DbContext>();
                DateTimeOffset stuckSince = DateTimeOffset.UtcNow.AddHours(-1);

                ProcessingQueue item = new(connectorId, pipelineId, 1, ProcessingStatus.Pending, "{}");
                item.MarkAsProcessing(stuckSince);
                item.SetCreatedAt(stuckSince);
                dbContext.Add(item);
                await dbContext.SaveChangesAsync();
            });

            await InScopeAsync(async serviceProvider =>
            {
                IQueueProcessorService service = serviceProvider.GetRequiredService<IQueueProcessorService>();

                int recovered = await service.RecoverStuckItems(TimeSpan.FromMinutes(10));

                recovered.Should().Be(1);

                DbContext dbContext = serviceProvider.GetRequiredService<DbContext>();
                ProcessingQueue stored = await dbContext.Set<ProcessingQueue>().AsNoTracking().FirstAsync();
                stored.Status.Should().Be(ProcessingStatus.Error);
                stored.LastError.Should().NotBeNullOrEmpty();
                stored.FinishedAt.Should().NotBeNull();
            });
        }

        [Test]
        public async Task RecoverStuckItems_does_not_touch_recent_processing_items()
        {
            await InScopeAsync(async serviceProvider =>
            {
                (long connectorId, long pipelineId) = await SeedConnectorAndPipeline(serviceProvider);
                DbContext dbContext = serviceProvider.GetRequiredService<DbContext>();

                ProcessingQueue item = new(connectorId, pipelineId, 1, ProcessingStatus.Pending, "{}");
                item.MarkAsProcessing(DateTimeOffset.UtcNow);
                item.SetCreatedAt(DateTimeOffset.UtcNow);
                dbContext.Add(item);
                await dbContext.SaveChangesAsync();
            });

            await InScopeAsync(async serviceProvider =>
            {
                IQueueProcessorService service = serviceProvider.GetRequiredService<IQueueProcessorService>();

                int recovered = await service.RecoverStuckItems(TimeSpan.FromMinutes(10));

                recovered.Should().Be(0);
            });
        }

        [Test]
        public async Task RecoverStuckExecutions_marks_running_executions_past_timeout_as_error()
        {
            await InScopeAsync(async serviceProvider =>
            {
                (long connectorId, long pipelineId) = await SeedConnectorAndPipeline(serviceProvider);
                DbContext dbContext = serviceProvider.GetRequiredService<DbContext>();
                DateTimeOffset stuckSince = DateTimeOffset.UtcNow.AddHours(-1);

                Execution execution = new(ExecutionType.Pipeline, connectorId, ExecutionStatus.Running, stuckSince, pipelineId);
                execution.SetCreatedAt(stuckSince);
                dbContext.Add(execution);
                await dbContext.SaveChangesAsync();
            });

            await InScopeAsync(async serviceProvider =>
            {
                IQueueProcessorService service = serviceProvider.GetRequiredService<IQueueProcessorService>();

                int recovered = await service.RecoverStuckExecutions(TimeSpan.FromMinutes(10));

                recovered.Should().Be(1);

                DbContext dbContext = serviceProvider.GetRequiredService<DbContext>();
                Execution stored = await dbContext.Set<Execution>().AsNoTracking().FirstAsync();
                stored.Status.Should().Be(ExecutionStatus.Error);
                stored.FinishedAt.Should().NotBeNull();
            });
        }

        [Test]
        public async Task GetPendingToProcess_returns_limited_items_ordered_by_priority()
        {
            await InScopeAsync(async serviceProvider =>
            {
                (long connectorId, long pipelineId) = await SeedConnectorAndPipeline(serviceProvider);
                DbContext dbContext = serviceProvider.GetRequiredService<DbContext>();
                DateTimeOffset now = DateTimeOffset.UtcNow;

                foreach (int priority in new[] { 3, 1, 2 })
                {
                    ProcessingQueue item = new(connectorId, pipelineId, priority, ProcessingStatus.Pending, "{}");
                    item.SetCreatedAt(now);
                    dbContext.Add(item);
                }
                await dbContext.SaveChangesAsync();
            });

            await InScopeAsync(async serviceProvider =>
            {
                IQueueProcessorService service = serviceProvider.GetRequiredService<IQueueProcessorService>();

                IReadOnlyCollection<ProcessingQueue> pending = await service.GetPendingToProcess(2);

                pending.Should().HaveCount(2);
                pending.Select(item => item.Priority).Should().Equal(1, 2);
            });
        }
    }
}
