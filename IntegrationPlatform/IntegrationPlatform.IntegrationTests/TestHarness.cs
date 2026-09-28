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
        public static string DbHost { get; private set; } = string.Empty;
        public static int DbPort { get; private set; }
        public const string DbName = "integrationtests";
        public const string DbUser = "testuser";
        public const string DbPass = "testpass";
        private static PostgreSqlContainer? container;

        [OneTimeSetUp]
        public async Task GlobalSetup()
        {
            container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase(DbName)
                .WithUsername(DbUser)
                .WithPassword(DbPass)
                .Build();

            await container.StartAsync();

            DbHost = container.Hostname;
            DbPort = container.GetMappedPublicPort(5432);

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
                "TRUNCATE callbackdelivery, executionlog, execution, processingqueue, " +
                "connectorattributevalue, pipelineroutine, pipelinestep, pipeline, connector, integration, " +
                "integrationservicecontract, servicecontract, integrationcategory, databasescript, databaseconnection, " +
                "apicall, javascriptfunction, integrationattribute " +
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

        // Pipeline com step JS + ServiceContract (HasCallback) vinculado + Connector com CallbackUrl,
        // para exercitar o enfileiramento de callback (ServiceCallbackDispatcher) ao concluir.
        protected static async Task<(long connectorId, long pipelineId)> SeedCallbackPipeline(IServiceProvider serviceProvider)
        {
            DbContext dbContext = serviceProvider.GetRequiredService<DbContext>();
            DateTimeOffset now = DateTimeOffset.UtcNow;

            IntegrationCategory category = new("cb-cat", "CB Cat");
            category.SetCreatedAt(now);
            dbContext.Add(category);
            await dbContext.SaveChangesAsync();

            ServiceContract contract = new("cb.service", "CB Service", category.Id, hasCallback: true);
            contract.SetCreatedAt(now);
            dbContext.Add(contract);
            await dbContext.SaveChangesAsync();

            Integration integration = new("cb-integration", "CB Integration");
            integration.SetCreatedAt(now);
            dbContext.Add(integration);
            await dbContext.SaveChangesAsync();

            Connector connector = new(integration.Id, "CB Connector");
            connector.SetCallback("https://consumer.example.com/callback", null);
            connector.SetCreatedAt(now);
            Pipeline pipeline = new(integration.Id, "cb-pipeline", "CB Pipeline");
            pipeline.BindServiceContract(contract.Id);
            pipeline.SetCreatedAt(now);
            JavaScriptFunction function = new("cb-fn", "result.value = { ok: true };");
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

        // Pipeline com step SQL (ExecuteScript) apontando para o PROPRIO Postgres de teste, exercitando
        // o PostgreSqlExecutor de verdade (sem mock). O SSRF guard nao se aplica a conexoes de banco.
        protected static async Task<(long connectorId, long pipelineId)> SeedSqlPipeline(IServiceProvider serviceProvider)
        {
            DbContext dbContext = serviceProvider.GetRequiredService<DbContext>();
            DateTimeOffset now = DateTimeOffset.UtcNow;

            Integration integration = new("sql-integration", "SQL Integration");
            integration.SetCreatedAt(now);
            dbContext.Add(integration);
            await dbContext.SaveChangesAsync();

            Connector connector = new(integration.Id, "SQL Connector");
            connector.SetCreatedAt(now);
            Pipeline pipeline = new(integration.Id, "sql-pipeline", "SQL Pipeline");
            pipeline.SetCreatedAt(now);
            DatabaseConnection databaseConnection = new("test-db", DatabaseType.PostgreSql, TestHarness.DbHost, TestHarness.DbPort, TestHarness.DbName, TestHarness.DbUser, TestHarness.DbPass);
            databaseConnection.SetCreatedAt(now);
            dbContext.Add(connector);
            dbContext.Add(pipeline);
            dbContext.Add(databaseConnection);
            await dbContext.SaveChangesAsync();

            DatabaseScript databaseScript = new(databaseConnection.Id, "test-script", "SELECT 1 AS n");
            databaseScript.SetCreatedAt(now);
            dbContext.Add(databaseScript);
            await dbContext.SaveChangesAsync();

            PipelineStep step = new(pipeline.Id, 1, "SQL Step", PipelineStepType.ExecuteScript, ErrorAction.Stop, databaseScriptId: databaseScript.Id);
            step.SetCreatedAt(now);
            dbContext.Add(step);
            await dbContext.SaveChangesAsync();

            return (connector.Id, pipeline.Id);
        }

        protected static async Task<long> ConnectorId(IServiceProvider serviceProvider)
        {
            DbContext dbContext = serviceProvider.GetRequiredService<DbContext>();
            return await dbContext.Set<Connector>().AsNoTracking().Select(item => item.Id).FirstAsync();
        }

        // Grafo minimo que o ResolveTarget percorre: categoria -> contrato -> vinculo integracao/contrato ->
        // pipeline do contrato, alem do proprio conector.
        protected static async Task SeedServiceContractGraph(IServiceProvider serviceProvider, bool integrationActive = true, bool connectorActive = true, string? inputSchema = null)
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

            ServiceContract contract = new("teste.contrato", "Contrato de Teste", category.Id, inputSchema: inputSchema);
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
