using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationPlatform.IntegrationTests
{
    [TestFixture]
    public sealed class ListQueriesIntegrationTests : IntegrationTestBase
    {
        [Test]
        public async Task GetActiveConnectors_and_pipelines_return_seeded()
        {
            await InScopeAsync(async serviceProvider =>
            {
                await SeedConnectorAndPipeline(serviceProvider);
            });

            await InScopeAsync(async serviceProvider =>
            {
                List<Connector> connectors = await serviceProvider.GetRequiredService<IConnectorService>().GetActiveConnectors();
                connectors.Should().NotBeEmpty();

                List<Pipeline> pipelines = await serviceProvider.GetRequiredService<IPipelineService>().GetActivePipelines();
                pipelines.Should().NotBeEmpty();
            });
        }

        [Test]
        public async Task GetActiveIntegrationCategories_returns_seeded()
        {
            await InScopeAsync(async serviceProvider =>
            {
                DbContext dbContext = serviceProvider.GetRequiredService<DbContext>();
                IntegrationCategory category = new("list-cat", "List Cat");
                category.SetCreatedAt(DateTimeOffset.UtcNow);
                dbContext.Add(category);
                await dbContext.SaveChangesAsync();
            });

            await InScopeAsync(async serviceProvider =>
            {
                List<IntegrationCategory> categories = await serviceProvider.GetRequiredService<IIntegrationCategoryService>().GetActiveIntegrationCategories();
                categories.Should().Contain(item => item.Identifier == "list-cat");
            });
        }

        [Test]
        public async Task GetActiveServiceContracts_returns_seeded()
        {
            await InScopeAsync(async serviceProvider =>
            {
                DbContext dbContext = serviceProvider.GetRequiredService<DbContext>();
                IntegrationCategory category = new("sc-list-cat", "SC List Cat");
                category.SetCreatedAt(DateTimeOffset.UtcNow);
                dbContext.Add(category);
                await dbContext.SaveChangesAsync();

                ServiceContract contract = new("sc.list", "SC List", category.Id);
                contract.SetCreatedAt(DateTimeOffset.UtcNow);
                dbContext.Add(contract);
                await dbContext.SaveChangesAsync();
            });

            await InScopeAsync(async serviceProvider =>
            {
                List<ServiceContract> contracts = await serviceProvider.GetRequiredService<IServiceContractService>().GetActiveServiceContracts();
                contracts.Should().Contain(item => item.Identifier == "sc.list");
            });
        }
    }
}
