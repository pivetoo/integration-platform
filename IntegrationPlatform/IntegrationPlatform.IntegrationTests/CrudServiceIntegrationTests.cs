using IntegrationPlatform.Application.Requests.Integrations;
using IntegrationPlatform.Application.Requests.JavaScriptFunctions;
using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationPlatform.IntegrationTests
{
    [TestFixture]
    public sealed class CrudServiceIntegrationTests : IntegrationTestBase
    {
        [Test]
        public async Task IntegrationService_creates_and_reads_back()
        {
            long id = 0;

            await InScopeAsync(async serviceProvider =>
            {
                IIntegrationService service = serviceProvider.GetRequiredService<IIntegrationService>();

                Integration created = await service.CreateIntegration(new CreateIntegrationRequest { Identifier = "stripe", Name = "Stripe" });

                created.Id.Should().BeGreaterThan(0);
                created.Identifier.Should().Be("stripe");
                id = created.Id;
            });

            await InScopeAsync(async serviceProvider =>
            {
                IIntegrationService service = serviceProvider.GetRequiredService<IIntegrationService>();

                Integration? fetched = await service.GetIntegrationById(id);

                fetched.Should().NotBeNull();
                fetched!.Name.Should().Be("Stripe");
            });
        }

        [Test]
        public async Task IntegrationService_lists_active_integrations()
        {
            await InScopeAsync(async serviceProvider =>
            {
                IIntegrationService service = serviceProvider.GetRequiredService<IIntegrationService>();
                await service.CreateIntegration(new CreateIntegrationRequest { Identifier = "active-one", Name = "Active One" });
            });

            await InScopeAsync(async serviceProvider =>
            {
                IIntegrationService service = serviceProvider.GetRequiredService<IIntegrationService>();

                List<Integration> active = await service.GetActiveIntegrations();

                active.Should().ContainSingle(item => item.Identifier == "active-one");
            });
        }

        [Test]
        public async Task JavaScriptFunctionService_creates_and_reads_back()
        {
            long id = 0;

            await InScopeAsync(async serviceProvider =>
            {
                IJavaScriptFunctionService service = serviceProvider.GetRequiredService<IJavaScriptFunctionService>();

                JavaScriptFunction created = await service.CreateJavaScriptFunction(new CreateJavaScriptFunctionRequest { Name = "transform", Code = "return payload;" });

                id = created.Id;
            });

            await InScopeAsync(async serviceProvider =>
            {
                IJavaScriptFunctionService service = serviceProvider.GetRequiredService<IJavaScriptFunctionService>();

                JavaScriptFunction? fetched = await service.GetJavaScriptFunctionById(id);

                fetched.Should().NotBeNull();
                fetched!.Code.Should().Be("return payload;");
            });
        }
    }
}
