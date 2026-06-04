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
    }
}
