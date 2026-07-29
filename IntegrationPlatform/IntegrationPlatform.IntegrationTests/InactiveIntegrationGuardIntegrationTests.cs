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

        private static async Task<long> ConnectorId(IServiceProvider serviceProvider)
        {
            DbContext dbContext = serviceProvider.GetRequiredService<DbContext>();
            return await dbContext.Set<Connector>().AsNoTracking().Select(item => item.Id).FirstAsync();
        }

        // Grafo minimo que o ResolveTarget percorre: categoria -> contrato -> vinculo integracao/contrato ->
        // pipeline do contrato, alem do proprio conector.
        private static async Task SeedServiceContractGraph(IServiceProvider serviceProvider, bool integrationActive, bool connectorActive)
        {
            DbContext dbContext = serviceProvider.GetRequiredService<DbContext>();
            DateTimeOffset now = DateTimeOffset.UtcNow;

            IntegrationCategory category = new("teste-categoria", "Categoria de Teste");
            category.SetCreatedAt(now);
            dbContext.Add(category);
            await dbContext.SaveChangesAsync();

            Integration integration = new("teste-integracao", "Integracao de Teste", null, category.Id);
            integration.SetCreatedAt(now);
            dbContext.Add(integration);

            ServiceContract contract = new("teste.contrato", "Contrato de Teste", category.Id);
            contract.SetCreatedAt(now);
            dbContext.Add(contract);
            await dbContext.SaveChangesAsync();

            if (!integrationActive)
            {
                integration.Update(integration.Identifier, integration.Name, integration.Description, category.Id, false, integration.IconUrl, integration.SupportsWebhook);
            }

            Connector connector = new(integration.Id, "Conector de Teste");
            connector.SetCreatedAt(now);
            if (!connectorActive)
            {
                connector.Update(integration.Id, connector.Name, connector.SystemApplicationId, false);
            }

            Pipeline pipeline = new(integration.Id, "teste-pipeline", "Pipeline de Teste");
            pipeline.BindServiceContract(contract.Id);
            pipeline.SetCreatedAt(now);

            IntegrationServiceContract binding = new(integration.Id, contract.Id);
            binding.SetCreatedAt(now);

            dbContext.Add(connector);
            dbContext.Add(pipeline);
            dbContext.Add(binding);
            await dbContext.SaveChangesAsync();
        }
    }
}
