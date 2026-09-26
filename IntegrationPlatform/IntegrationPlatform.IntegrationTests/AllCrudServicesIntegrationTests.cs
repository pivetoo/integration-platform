using IntegrationPlatform.Application.Models;
using IntegrationPlatform.Application.Requests.ApiCalls;
using IntegrationPlatform.Application.Requests.Connectors;
using IntegrationPlatform.Application.Requests.DatabaseConnections;
using IntegrationPlatform.Application.Requests.DatabaseScripts;
using IntegrationPlatform.Application.Requests.IntegrationCategories;
using IntegrationPlatform.Application.Requests.PipelineRoutines;
using IntegrationPlatform.Application.Requests.Pipelines;
using IntegrationPlatform.Application.Requests.PipelineSteps;
using IntegrationPlatform.Application.Requests.ServiceContracts;
using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using IntegrationPlatform.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationPlatform.IntegrationTests
{
    [TestFixture]
    public sealed class AllCrudServicesIntegrationTests : IntegrationTestBase
    {
        private static async Task<long> SeedIntegration(IServiceProvider serviceProvider)
        {
            DbContext dbContext = serviceProvider.GetRequiredService<DbContext>();
            Integration integration = new("seed-integration", "Seed Integration");
            integration.SetCreatedAt(DateTimeOffset.UtcNow);
            dbContext.Add(integration);
            await dbContext.SaveChangesAsync();
            return integration.Id;
        }

        [Test]
        public async Task ApiCallService_creates_and_reads_back()
        {
            await InScopeAsync(async serviceProvider =>
            {
                IApiCallService service = serviceProvider.GetRequiredService<IApiCallService>();
                ApiCall created = await service.CreateApiCall(new CreateApiCallRequest { Name = "Charge", Method = 2, Url = "https://api.example.com/charge" });
                created.Id.Should().BeGreaterThan(0);

                ApiCall? fetched = await service.GetApiCallById(created.Id);
                fetched.Should().NotBeNull();
                fetched!.Url.Should().Be("https://api.example.com/charge");
            });
        }

        [Test]
        public async Task DatabaseConnectionService_creates_and_reads_back()
        {
            await InScopeAsync(async serviceProvider =>
            {
                IDatabaseConnectionService service = serviceProvider.GetRequiredService<IDatabaseConnectionService>();
                DatabaseConnection created = await service.CreateDatabaseConnection(new CreateDatabaseConnectionRequest { Name = "prod", Type = 1, Host = "db.host", Port = 5432, Database = "app", Username = "user", Password = "pass" });

                DatabaseConnection? fetched = await service.GetDatabaseConnectionById(created.Id);
                fetched.Should().NotBeNull();
                fetched!.Host.Should().Be("db.host");
            });
        }

        [Test]
        public async Task IntegrationCategoryService_creates_and_reads_back()
        {
            await InScopeAsync(async serviceProvider =>
            {
                IIntegrationCategoryService service = serviceProvider.GetRequiredService<IIntegrationCategoryService>();
                IntegrationCategory created = await service.CreateIntegrationCategory(new CreateIntegrationCategoryRequest { Identifier = "custom-cat", Name = "Custom" });

                IntegrationCategory? fetched = await service.GetIntegrationCategoryById(created.Id);
                fetched.Should().NotBeNull();
                fetched!.Identifier.Should().Be("custom-cat");
            });
        }

        [Test]
        public async Task ServiceContractService_creates_with_category()
        {
            await InScopeAsync(async serviceProvider =>
            {
                IIntegrationCategoryService categoryService = serviceProvider.GetRequiredService<IIntegrationCategoryService>();
                IntegrationCategory category = await categoryService.CreateIntegrationCategory(new CreateIntegrationCategoryRequest { Identifier = "sc-cat", Name = "SC Cat" });

                IServiceContractService service = serviceProvider.GetRequiredService<IServiceContractService>();
                ServiceContract created = await service.CreateServiceContract(new CreateServiceContractRequest { Identifier = "custom.do", Name = "Custom Do", IntegrationCategoryId = category.Id, HasCallback = true });

                ServiceContract? fetched = await service.GetServiceContractById(created.Id);
                fetched.Should().NotBeNull();
                fetched!.HasCallback.Should().BeTrue();
            });
        }

        [Test]
        public async Task ConnectorService_creates_with_integration()
        {
            await InScopeAsync(async serviceProvider =>
            {
                long integrationId = await SeedIntegration(serviceProvider);
                IConnectorService service = serviceProvider.GetRequiredService<IConnectorService>();

                Connector created = await service.CreateConnector(new CreateConnectorRequest { IntegrationId = integrationId, Name = "My Connector" });

                Connector? fetched = await service.GetConnectorById(created.Id);
                fetched.Should().NotBeNull();
                fetched!.Name.Should().Be("My Connector");
            });
        }

        [Test]
        public async Task PipelineService_creates_with_integration()
        {
            await InScopeAsync(async serviceProvider =>
            {
                long integrationId = await SeedIntegration(serviceProvider);
                IPipelineService service = serviceProvider.GetRequiredService<IPipelineService>();

                Pipeline created = await service.CreatePipeline(new CreatePipelineRequest { IntegrationId = integrationId, Identifier = "my-flow", Name = "My Flow" });

                Pipeline? fetched = await service.GetPipelineById(created.Id);
                fetched.Should().NotBeNull();
                fetched!.Identifier.Should().Be("my-flow");
            });
        }

        [Test]
        public async Task DatabaseScriptService_creates_with_connection()
        {
            await InScopeAsync(async serviceProvider =>
            {
                IDatabaseConnectionService connectionService = serviceProvider.GetRequiredService<IDatabaseConnectionService>();
                DatabaseConnection connection = await connectionService.CreateDatabaseConnection(new CreateDatabaseConnectionRequest { Name = "ds-conn", Type = 1, Host = "h", Port = 5432, Database = "d", Username = "u", Password = "p" });

                IDatabaseScriptService service = serviceProvider.GetRequiredService<IDatabaseScriptService>();
                DatabaseScript created = await service.CreateDatabaseScript(new CreateDatabaseScriptRequest { DatabaseConnectionId = connection.Id, Name = "Report", Script = "SELECT 1" });

                DatabaseScript? fetched = await service.GetDatabaseScriptById(created.Id);
                fetched.Should().NotBeNull();
                fetched!.Script.Should().Be("SELECT 1");
            });
        }


        [Test]
        public async Task PipelineStepService_creates_with_pipeline()
        {
            await InScopeAsync(async serviceProvider =>
            {
                (_, long pipelineId) = await SeedConnectorAndPipeline(serviceProvider);
                DbContext dbContext = serviceProvider.GetRequiredService<DbContext>();
                JavaScriptFunction function = new("step-fn", "return payload;");
                function.SetCreatedAt(DateTimeOffset.UtcNow);
                dbContext.Add(function);
                await dbContext.SaveChangesAsync();

                IPipelineStepService service = serviceProvider.GetRequiredService<IPipelineStepService>();

                PipelineStep created = await service.CreatePipelineStep(new CreatePipelineStepRequest { PipelineId = pipelineId, Order = 1, Name = "Step 1", Type = PipelineStepType.JavaScriptFunction, ErrorAction = ErrorAction.Stop, JavaScriptFunctionId = function.Id });

                PipelineStep? fetched = await service.GetPipelineStepById(created.Id);
                fetched.Should().NotBeNull();
                fetched!.Name.Should().Be("Step 1");
            });
        }

        [Test]
        public async Task PipelineRoutineService_creates_with_connector_and_pipeline()
        {
            await InScopeAsync(async serviceProvider =>
            {
                (long connectorId, long pipelineId) = await SeedConnectorAndPipeline(serviceProvider);
                IPipelineRoutineService service = serviceProvider.GetRequiredService<IPipelineRoutineService>();

                PipelineRoutine created = await service.CreatePipelineRoutine(new CreatePipelineRoutineRequest { ConnectorId = connectorId, PipelineId = pipelineId, IntervalMinutes = 15 });

                PipelineRoutine? fetched = await service.GetPipelineRoutineById(created.Id);
                fetched.Should().NotBeNull();
                fetched!.IntervalInMinutes.Should().Be(15);
            });
        }

        [Test]
        public async Task DashboardService_returns_data()
        {
            await InScopeAsync(async serviceProvider =>
            {
                IDashboardService service = serviceProvider.GetRequiredService<IDashboardService>();

                DashboardData data = await service.GetDashboardData();

                data.Should().NotBeNull();
                data.QueueByStatus.Should().NotBeNull();
                data.TopConnectors.Should().NotBeNull();
            });
        }
    }
}
