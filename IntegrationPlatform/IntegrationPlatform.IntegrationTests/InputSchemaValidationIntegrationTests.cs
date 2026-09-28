using Archon.Core.Exceptions;
using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using IntegrationPlatform.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationPlatform.IntegrationTests
{
    [TestFixture]
    public sealed class InputSchemaValidationIntegrationTests : IntegrationTestBase
    {
        private const string Schema = "{\"pixKey\":\"string\",\"netAmount\":\"number\",\"note\":\"string?\"}";

        [Test]
        public async Task EnqueueService_rejects_missing_required_field_and_does_not_enqueue()
        {
            await InScopeAsync(async serviceProvider =>
            {
                await SeedServiceContractGraph(serviceProvider, inputSchema: Schema);
                IServiceExecutionService service = serviceProvider.GetRequiredService<IServiceExecutionService>();
                long connectorId = await ConnectorId(serviceProvider);

                Func<Task> act = () => service.EnqueueService("teste.contrato", connectorId, "{\"netAmount\":10}");

                BusinessRuleException exception = (await act.Should().ThrowAsync<BusinessRuleException>()).Which;
                exception.Message.Should().Be("serviceContract.input.invalid");
                exception.MessageArgs[0].ToString().Should().Contain("pixKey: required");

                DbContext dbContext = serviceProvider.GetRequiredService<DbContext>();
                (await dbContext.Set<ProcessingQueue>().CountAsync()).Should().Be(0);
            });
        }

        [Test]
        public async Task ExecuteService_rejects_wrong_type_before_running_the_pipeline()
        {
            await InScopeAsync(async serviceProvider =>
            {
                await SeedServiceContractGraph(serviceProvider, inputSchema: Schema);
                IServiceExecutionService service = serviceProvider.GetRequiredService<IServiceExecutionService>();
                long connectorId = await ConnectorId(serviceProvider);

                Func<Task> act = () => service.ExecuteService("teste.contrato", connectorId, "{\"pixKey\":\"k\",\"netAmount\":\"abc\"}");

                BusinessRuleException exception = (await act.Should().ThrowAsync<BusinessRuleException>()).Which;
                exception.MessageArgs[0].ToString().Should().Contain("netAmount: expected number");

                DbContext dbContext = serviceProvider.GetRequiredService<DbContext>();
                (await dbContext.Set<Execution>().CountAsync()).Should().Be(0);
            });
        }

        [Test]
        public async Task EnqueueService_accepts_valid_input()
        {
            await InScopeAsync(async serviceProvider =>
            {
                await SeedServiceContractGraph(serviceProvider, inputSchema: Schema);
                IServiceExecutionService service = serviceProvider.GetRequiredService<IServiceExecutionService>();

                ProcessingQueue queued = await service.EnqueueService("teste.contrato", await ConnectorId(serviceProvider), "{\"pixKey\":\"k\",\"netAmount\":10}");

                queued.Status.Should().Be(ProcessingStatus.Pending);
            });
        }

        [Test]
        public async Task EnqueueService_with_empty_schema_accepts_any_object()
        {
            await InScopeAsync(async serviceProvider =>
            {
                await SeedServiceContractGraph(serviceProvider, inputSchema: null);
                IServiceExecutionService service = serviceProvider.GetRequiredService<IServiceExecutionService>();

                ProcessingQueue queued = await service.EnqueueService("teste.contrato", await ConnectorId(serviceProvider), "{\"qualquer\":1}");

                queued.Status.Should().Be(ProcessingStatus.Pending);
            });
        }

        [Test]
        public async Task EnqueueService_with_null_input_fails_when_schema_has_required_fields()
        {
            await InScopeAsync(async serviceProvider =>
            {
                await SeedServiceContractGraph(serviceProvider, inputSchema: Schema);
                IServiceExecutionService service = serviceProvider.GetRequiredService<IServiceExecutionService>();
                long connectorId = await ConnectorId(serviceProvider);

                Func<Task> act = () => service.EnqueueService("teste.contrato", connectorId, null);

                BusinessRuleException exception = (await act.Should().ThrowAsync<BusinessRuleException>()).Which;
                exception.MessageArgs[0].ToString().Should().Contain("pixKey: required").And.Contain("netAmount: required");
            });
        }

        [Test]
        public async Task EnqueueService_with_all_optional_schema_accepts_null_input()
        {
            await InScopeAsync(async serviceProvider =>
            {
                await SeedServiceContractGraph(serviceProvider, inputSchema: "{\"a\":\"string?\"}");
                IServiceExecutionService service = serviceProvider.GetRequiredService<IServiceExecutionService>();

                ProcessingQueue queued = await service.EnqueueService("teste.contrato", await ConnectorId(serviceProvider), null);

                queued.Status.Should().Be(ProcessingStatus.Pending);
            });
        }
    }
}
