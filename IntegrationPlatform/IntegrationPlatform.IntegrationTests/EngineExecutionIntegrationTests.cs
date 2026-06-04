using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using IntegrationPlatform.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationPlatform.IntegrationTests
{
    [TestFixture]
    public sealed class EngineExecutionIntegrationTests : IntegrationTestBase
    {
        [Test]
        public async Task ExecutePipeline_runs_javascript_step_and_completes_successfully()
        {
            long connectorId = 0;
            long pipelineId = 0;

            await InScopeAsync(async serviceProvider =>
            {
                (connectorId, pipelineId) = await SeedJavaScriptPipeline(serviceProvider, "result.value = { ok: true };");
            });

            await InScopeAsync(async serviceProvider =>
            {
                IExecutionEngineService engine = serviceProvider.GetRequiredService<IExecutionEngineService>();

                Execution execution = await engine.ExecutePipeline(connectorId, pipelineId, "{}", ExecutionType.Manual);

                execution.Status.Should().Be(ExecutionStatus.Success);
                execution.FinishedAt.Should().NotBeNull();

                DbContext dbContext = serviceProvider.GetRequiredService<DbContext>();
                Execution stored = await dbContext.Set<Execution>().AsNoTracking().FirstAsync(item => item.Id == execution.Id);
                stored.Status.Should().Be(ExecutionStatus.Success);
            });
        }

        [Test]
        public async Task ExecutePipeline_with_failing_javascript_step_marks_execution_error()
        {
            long connectorId = 0;
            long pipelineId = 0;

            await InScopeAsync(async serviceProvider =>
            {
                (connectorId, pipelineId) = await SeedJavaScriptPipeline(serviceProvider, "throw new Error('boom');");
            });

            await InScopeAsync(async serviceProvider =>
            {
                IExecutionEngineService engine = serviceProvider.GetRequiredService<IExecutionEngineService>();

                Execution execution = await engine.ExecutePipeline(connectorId, pipelineId, "{}", ExecutionType.Manual);

                execution.Status.Should().Be(ExecutionStatus.Error);
            });
        }

        [Test]
        public async Task ExecutePipeline_with_callback_contract_enqueues_callback_delivery()
        {
            long connectorId = 0;
            long pipelineId = 0;

            await InScopeAsync(async serviceProvider =>
            {
                (connectorId, pipelineId) = await SeedCallbackPipeline(serviceProvider);
            });

            await InScopeAsync(async serviceProvider =>
            {
                IExecutionEngineService engine = serviceProvider.GetRequiredService<IExecutionEngineService>();

                Execution execution = await engine.ExecutePipeline(connectorId, pipelineId, "{}", ExecutionType.Manual);

                execution.Status.Should().Be(ExecutionStatus.Success);

                DbContext dbContext = serviceProvider.GetRequiredService<DbContext>();
                CallbackDelivery? delivery = await dbContext.Set<CallbackDelivery>().AsNoTracking()
                    .FirstOrDefaultAsync(item => item.ExecutionId == execution.Id);

                delivery.Should().NotBeNull();
                delivery!.Status.Should().Be(CallbackDeliveryStatus.Pending);
                delivery.CallbackUrl.Should().Be("https://consumer.example.com/callback");
            });
        }
    }
}
