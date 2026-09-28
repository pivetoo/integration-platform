using IntegrationPlatform.Application.Requests.ApiCalls;
using IntegrationPlatform.Application.Requests.IntegrationCategories;
using IntegrationPlatform.Application.Requests.Integrations;
using IntegrationPlatform.Application.Requests.JavaScriptFunctions;
using IntegrationPlatform.Application.Requests.Pipelines;
using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationPlatform.IntegrationTests
{
    [TestFixture]
    public sealed class CrudUpdateDeleteIntegrationTests : IntegrationTestBase
    {
        [Test]
        public async Task IntegrationService_update_and_delete()
        {
            await InScopeAsync(async serviceProvider =>
            {
                IIntegrationService service = serviceProvider.GetRequiredService<IIntegrationService>();
                Integration created = await service.CreateIntegration(new CreateIntegrationRequest { Identifier = "crud-int", Name = "Crud" });

                Integration updated = await service.UpdateIntegration(created.Id, new UpdateIntegrationRequest { Id = created.Id, Identifier = "crud-int", Name = "Crud Updated", IsActive = false, SupportsWebhook = true });
                updated.Name.Should().Be("Crud Updated");
                updated.SupportsWebhook.Should().BeTrue();

                await service.Delete(created.Id);
                (await service.GetIntegrationById(created.Id)).Should().BeNull();
            });
        }

        [Test]
        public async Task ApiCallService_update()
        {
            await InScopeAsync(async serviceProvider =>
            {
                IApiCallService service = serviceProvider.GetRequiredService<IApiCallService>();
                ApiCall created = await service.CreateApiCall(new CreateApiCallRequest { Name = "Old", Method = 1, Url = "https://api.example.com/a" });

                ApiCall updated = await service.UpdateApiCall(created.Id, new UpdateApiCallRequest { Id = created.Id, Name = "New", Method = 5, Url = "https://api.example.com/b" });
                updated.Name.Should().Be("New");
                updated.Url.Should().Be("https://api.example.com/b");
            });
        }

        [Test]
        public async Task IntegrationCategoryService_update()
        {
            await InScopeAsync(async serviceProvider =>
            {
                IIntegrationCategoryService service = serviceProvider.GetRequiredService<IIntegrationCategoryService>();
                IntegrationCategory created = await service.CreateIntegrationCategory(new CreateIntegrationCategoryRequest { Identifier = "upd-cat", Name = "Old Cat" });

                IntegrationCategory updated = await service.UpdateIntegrationCategory(created.Id, new UpdateIntegrationCategoryRequest { Id = created.Id, Identifier = "upd-cat", Name = "New Cat", IsActive = true });
                updated.Name.Should().Be("New Cat");
            });
        }

        [Test]
        public async Task JavaScriptFunctionService_update_and_delete()
        {
            await InScopeAsync(async serviceProvider =>
            {
                IJavaScriptFunctionService service = serviceProvider.GetRequiredService<IJavaScriptFunctionService>();
                JavaScriptFunction created = await service.CreateJavaScriptFunction(new CreateJavaScriptFunctionRequest { Name = "fn", Code = "return 1;" });

                JavaScriptFunction updated = await service.UpdateJavaScriptFunction(created.Id, new UpdateJavaScriptFunctionRequest { Id = created.Id, Name = "fn2", Code = "return 2;" });
                updated.Code.Should().Be("return 2;");

                await service.Delete(created.Id);
                (await service.GetJavaScriptFunctionById(created.Id)).Should().BeNull();
            });
        }

        [Test]
        public async Task PipelineService_persists_max_attempts()
        {
            await InScopeAsync(async serviceProvider =>
            {
                IIntegrationService integrations = serviceProvider.GetRequiredService<IIntegrationService>();
                IPipelineService pipelines = serviceProvider.GetRequiredService<IPipelineService>();
                Integration integration = await integrations.CreateIntegration(new CreateIntegrationRequest { Identifier = "max-att", Name = "Max Attempts" });

                Pipeline created = await pipelines.CreatePipeline(new CreatePipelineRequest { IntegrationId = integration.Id, Identifier = "p-max", Name = "P Max", MaxAttempts = 3 });
                created.MaxAttempts.Should().Be(3);

                Pipeline updated = await pipelines.UpdatePipeline(created.Id, new UpdatePipelineRequest { Id = created.Id, IntegrationId = integration.Id, Identifier = "p-max", Name = "P Max", IsActive = true, MaxAttempts = 5 });
                updated.MaxAttempts.Should().Be(5);

                Pipeline withDefault = await pipelines.CreatePipeline(new CreatePipelineRequest { IntegrationId = integration.Id, Identifier = "p-default", Name = "P Default" });
                withDefault.MaxAttempts.Should().Be(1);
            });
        }
    }
}
