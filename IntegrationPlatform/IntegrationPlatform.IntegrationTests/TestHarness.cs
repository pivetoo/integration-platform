using IntegrationPlatform.Domain.Entities;
using IntegrationPlatform.Domain.ValueObjects;
using IntegrationPlatform.Infrastructure.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace IntegrationPlatform.IntegrationTests
{
    // Sobe um Postgres efemero (Testcontainers) UMA vez para toda a suite, roda as migrations reais
    // e expoe um ServiceProvider com a Infrastructure real do IntegrationPlatform. Com um unico tenant
    // configurado, o ResolveCurrentTenant do Archon resolve o DbContext automaticamente (single-tenant).
    // Requer Docker no host (disponivel nos runners do GitHub Actions; nao roda em sandbox sem Docker).
    [SetUpFixture]
    public sealed class TestHarness
    {
        public static IServiceProvider Services { get; private set; } = null!;
        private static PostgreSqlContainer? container;

        [OneTimeSetUp]
        public async Task GlobalSetup()
        {
            container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .Build();

            await container.StartAsync();

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["RunMigrations"] = "true",
                    ["TenantDatabases:test:ConnectionString"] = container.GetConnectionString(),
                    ["TenantDatabases:test:DatabaseType"] = "PostgreSql",
                    ["TenantDatabases:test:Schema"] = "public",
                })
                .Build();

            ServiceCollection services = new();
            services.AddLogging();
            // IStringLocalizer e usado pelos servicos do motor; no app real vem do AddArchonApi.
            services.AddLocalization();
            // Roda as migrations contra o container e registra persistencia + servicos reais.
            services.AddIntegrationPlatformInfrastructure(configuration);

            Services = services.BuildServiceProvider();
        }

        [OneTimeTearDown]
        public async Task GlobalTeardown()
        {
            if (Services is IAsyncDisposable disposableProvider)
            {
                await disposableProvider.DisposeAsync();
            }

            if (container is not null)
            {
                await container.DisposeAsync();
            }
        }
    }

    public abstract class IntegrationTestBase
    {
        // Limpa o grafo operacional antes de cada teste (container compartilhado entre testes).
        [SetUp]
        public async Task ResetDatabase()
        {
            using IServiceScope scope = TestHarness.Services.CreateScope();
            DbContext dbContext = scope.ServiceProvider.GetRequiredService<DbContext>();
            await dbContext.Database.ExecuteSqlRawAsync(
                "TRUNCATE callbackdelivery, executionlog, execution, processingqueue, reference, " +
                "connectorattributevalue, pipelineroutine, pipelinestep, pipeline, connector, integration " +
                "RESTART IDENTITY CASCADE");
        }

        protected static async Task InScopeAsync(Func<IServiceProvider, Task> action)
        {
            using IServiceScope scope = TestHarness.Services.CreateScope();
            await action(scope.ServiceProvider);
        }

        // Semeia o grafo minimo (Integration -> Connector + Pipeline) e devolve os ids, ja que
        // processingqueue/execution tem FK para connector e pipeline.
        protected static async Task<(long connectorId, long pipelineId)> SeedConnectorAndPipeline(IServiceProvider serviceProvider)
        {
            DbContext dbContext = serviceProvider.GetRequiredService<DbContext>();
            DateTimeOffset now = DateTimeOffset.UtcNow;

            Integration integration = new("test-integration", "Test Integration");
            integration.SetCreatedAt(now);
            dbContext.Add(integration);
            await dbContext.SaveChangesAsync();

            Connector connector = new(integration.Id, "Test Connector");
            connector.SetCreatedAt(now);
            Pipeline pipeline = new(integration.Id, "test-pipeline", "Test Pipeline");
            pipeline.SetCreatedAt(now);
            dbContext.Add(connector);
            dbContext.Add(pipeline);
            await dbContext.SaveChangesAsync();

            return (connector.Id, pipeline.Id);
        }

        // Semeia um connector + pipeline com UM step JavaScript que roda o codigo informado.
        // Cobre a execucao real do motor (ExecutePipeline -> StepExecutor JS -> persistencia).
        protected static async Task<(long connectorId, long pipelineId)> SeedJavaScriptPipeline(IServiceProvider serviceProvider, string jsCode)
        {
            DbContext dbContext = serviceProvider.GetRequiredService<DbContext>();
            DateTimeOffset now = DateTimeOffset.UtcNow;

            Integration integration = new("test-integration", "Test Integration");
            integration.SetCreatedAt(now);
            dbContext.Add(integration);
            await dbContext.SaveChangesAsync();

            Connector connector = new(integration.Id, "Test Connector");
            connector.SetCreatedAt(now);
            Pipeline pipeline = new(integration.Id, "test-pipeline", "Test Pipeline");
            pipeline.SetCreatedAt(now);
            JavaScriptFunction function = new("transform", jsCode);
            function.SetCreatedAt(now);
            dbContext.Add(connector);
            dbContext.Add(pipeline);
            dbContext.Add(function);
            await dbContext.SaveChangesAsync();

            PipelineStep step = new(pipeline.Id, 1, "JS Step", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, javaScriptFunctionId: function.Id);
            step.SetCreatedAt(now);
            dbContext.Add(step);
            await dbContext.SaveChangesAsync();

            return (connector.Id, pipeline.Id);
        }
    }
}
