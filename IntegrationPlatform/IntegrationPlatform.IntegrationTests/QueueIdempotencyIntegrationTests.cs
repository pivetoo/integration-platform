using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationPlatform.IntegrationTests
{
    [TestFixture]
    public sealed class QueueIdempotencyIntegrationTests : IntegrationTestBase
    {
        private static async Task<int> CountQueueItems(IServiceProvider serviceProvider)
        {
            DbContext dbContext = serviceProvider.GetRequiredService<DbContext>();
            return await dbContext.Set<ProcessingQueue>().AsNoTracking().CountAsync();
        }

        [Test]
        public async Task Enqueue_with_same_key_returns_same_item_and_inserts_once()
        {
            await InScopeAsync(async serviceProvider =>
            {
                (long connectorId, long pipelineId) = await SeedConnectorAndPipeline(serviceProvider);
                IExecutionService service = serviceProvider.GetRequiredService<IExecutionService>();

                ProcessingQueue first = await service.EnqueuePipeline(connectorId, pipelineId, "{}", 1, "key-1");
                ProcessingQueue second = await service.EnqueuePipeline(connectorId, pipelineId, "{}", 1, "key-1");

                second.Id.Should().Be(first.Id);
                (await CountQueueItems(serviceProvider)).Should().Be(1);
            });
        }

        [Test]
        public async Task Enqueue_without_key_always_inserts()
        {
            await InScopeAsync(async serviceProvider =>
            {
                (long connectorId, long pipelineId) = await SeedConnectorAndPipeline(serviceProvider);
                IExecutionService service = serviceProvider.GetRequiredService<IExecutionService>();

                await service.EnqueuePipeline(connectorId, pipelineId, "{}", 1);
                await service.EnqueuePipeline(connectorId, pipelineId, "{}", 1);

                (await CountQueueItems(serviceProvider)).Should().Be(2);
            });
        }

        [Test]
        public async Task Blank_key_is_treated_as_no_key()
        {
            await InScopeAsync(async serviceProvider =>
            {
                (long connectorId, long pipelineId) = await SeedConnectorAndPipeline(serviceProvider);
                IExecutionService service = serviceProvider.GetRequiredService<IExecutionService>();

                await service.EnqueuePipeline(connectorId, pipelineId, "{}", 1, "   ");
                await service.EnqueuePipeline(connectorId, pipelineId, "{}", 1, "   ");

                (await CountQueueItems(serviceProvider)).Should().Be(2);
            });
        }

        [Test]
        public async Task Same_key_on_different_pipeline_or_connector_does_not_collide()
        {
            await InScopeAsync(async serviceProvider =>
            {
                (long connectorId, long pipelineId) = await SeedConnectorAndPipeline(serviceProvider);
                DbContext dbContext = serviceProvider.GetRequiredService<DbContext>();
                long integrationId = (await dbContext.Set<Pipeline>().AsNoTracking().FirstAsync(item => item.Id == pipelineId)).IntegrationId;

                Pipeline otherPipeline = new(integrationId, "other-pipeline", "Other Pipeline");
                otherPipeline.SetCreatedAt(DateTimeOffset.UtcNow);
                Connector otherConnector = new(integrationId, "Other Connector");
                otherConnector.SetCreatedAt(DateTimeOffset.UtcNow);
                dbContext.Add(otherPipeline);
                dbContext.Add(otherConnector);
                await dbContext.SaveChangesAsync();

                IExecutionService service = serviceProvider.GetRequiredService<IExecutionService>();

                ProcessingQueue original = await service.EnqueuePipeline(connectorId, pipelineId, "{}", 1, "shared");
                ProcessingQueue otherByPipeline = await service.EnqueuePipeline(connectorId, otherPipeline.Id, "{}", 1, "shared");
                ProcessingQueue otherByConnector = await service.EnqueuePipeline(otherConnector.Id, pipelineId, "{}", 1, "shared");

                new[] { original.Id, otherByPipeline.Id, otherByConnector.Id }.Distinct().Should().HaveCount(3);
                (await CountQueueItems(serviceProvider)).Should().Be(3);
            });
        }

        [Test]
        public async Task Concurrent_enqueue_with_same_key_yields_single_item()
        {
            long connectorId = 0;
            long pipelineId = 0;
            await InScopeAsync(async serviceProvider => (connectorId, pipelineId) = await SeedConnectorAndPipeline(serviceProvider));

            long[] ids = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => Task.Run(async () =>
            {
                long id = 0;
                await InScopeAsync(async serviceProvider =>
                {
                    IExecutionService service = serviceProvider.GetRequiredService<IExecutionService>();
                    id = (await service.EnqueuePipeline(connectorId, pipelineId, "{}", 1, "race")).Id;
                });
                return id;
            })));

            ids.Distinct().Should().HaveCount(1);
            await InScopeAsync(async serviceProvider => (await CountQueueItems(serviceProvider)).Should().Be(1));
        }
    }
}
