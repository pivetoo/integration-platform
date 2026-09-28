using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using IntegrationPlatform.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationPlatform.IntegrationTests
{
    // Desativar conector/integracao precisa valer como kill switch de execucao, e nao apenas como filtro
    // de catalogo: antes destas guardas uma integracao inativa sumia das listagens mas o consumidor
    // continuava disparando chamada real ao provedor pelo contrato de servico.
    [TestFixture]
    public sealed class InactiveIntegrationGuardIntegrationTests : IntegrationTestBase
    {
        [Test]
        public async Task EnqueueService_should_refuse_when_the_integration_is_inactive()
        {
            await InScopeAsync(async serviceProvider =>
            {
                await SeedServiceContractGraph(serviceProvider, integrationActive: false, connectorActive: true);

                IServiceExecutionService service = serviceProvider.GetRequiredService<IServiceExecutionService>();
                long connectorId = await ConnectorId(serviceProvider);

                Func<Task> act = () => service.EnqueueService("teste.contrato", connectorId, "{}");

                await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("integration.notActive");
            });

            await InScopeAsync(async serviceProvider =>
            {
                DbContext dbContext = serviceProvider.GetRequiredService<DbContext>();
                (await dbContext.Set<ProcessingQueue>().CountAsync()).Should().Be(0);
            });
        }

        [Test]
        public async Task EnqueueService_should_refuse_when_the_connector_is_inactive()
        {
            await InScopeAsync(async serviceProvider =>
            {
                await SeedServiceContractGraph(serviceProvider, integrationActive: true, connectorActive: false);

                IServiceExecutionService service = serviceProvider.GetRequiredService<IServiceExecutionService>();
                long connectorId = await ConnectorId(serviceProvider);

                Func<Task> act = () => service.EnqueueService("teste.contrato", connectorId, "{}");

                await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("connector.notActive");
            });
        }

        [Test]
        public async Task EnqueueService_should_accept_when_connector_and_integration_are_active()
        {
            await InScopeAsync(async serviceProvider =>
            {
                await SeedServiceContractGraph(serviceProvider, integrationActive: true, connectorActive: true);

                IServiceExecutionService service = serviceProvider.GetRequiredService<IServiceExecutionService>();

                ProcessingQueue queued = await service.EnqueueService("teste.contrato", await ConnectorId(serviceProvider), "{}");

                queued.Status.Should().Be(ProcessingStatus.Pending);
            });
        }

        // Kill switch tardio: item enfileirado antes de a integracao ser desativada nao pode executar quando
        // o worker o retira da fila.
        [Test]
        public async Task ProcessItem_should_fail_the_item_when_the_integration_was_deactivated_after_enqueue()
        {
            long queueItemId = 0;

            await InScopeAsync(async serviceProvider =>
            {
                (long connectorId, long pipelineId) = await SeedConnectorAndPipeline(serviceProvider);
                DbContext dbContext = serviceProvider.GetRequiredService<DbContext>();

                ProcessingQueue item = new(connectorId, pipelineId, 1, ProcessingStatus.Pending, "{}");
                item.SetCreatedAt(DateTimeOffset.UtcNow);
                dbContext.Add(item);
                await dbContext.SaveChangesAsync();
                queueItemId = item.Id;

                Integration integration = await dbContext.Set<Integration>().AsTracking().FirstAsync();
                integration.Update(integration.Identifier, integration.Name, integration.Description, integration.IntegrationCategoryId, false, integration.IconUrl, integration.SupportsWebhook);
                await dbContext.SaveChangesAsync();
            });

            await InScopeAsync(async serviceProvider =>
            {
                IQueueProcessorService service = serviceProvider.GetRequiredService<IQueueProcessorService>();

                await service.ProcessItem(queueItemId);

                DbContext dbContext = serviceProvider.GetRequiredService<DbContext>();
                ProcessingQueue stored = await dbContext.Set<ProcessingQueue>().AsNoTracking().FirstAsync(item => item.Id == queueItemId);
                stored.Status.Should().Be(ProcessingStatus.Error);
                stored.LastError.Should().Contain("inativa");

                // Nao pode ter havido execucao: o objetivo da guarda e nao chegar ao provedor.
                (await dbContext.Set<Execution>().CountAsync()).Should().Be(0);
            });
        }
    }
}
