using IntegrationPlatform.Application.Models;
using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using IntegrationPlatform.Domain.ValueObjects;
using IntegrationPlatform.Infrastructure.Services.ExecutionEngine;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationPlatform.IntegrationTests
{
    [TestFixture]
    public sealed class QueueRetryIntegrationTests : IntegrationTestBase
    {
        private sealed class ScriptedStepExecutor : IStepExecutorService
        {
            private readonly Func<PipelineStep, PipelineStepExecutionResult> handler;

            public ScriptedStepExecutor(Func<PipelineStep, PipelineStepExecutionResult> handler)
            {
                this.handler = handler;
            }

            public List<string> ExecutedSteps { get; } = [];

            public Dictionary<string, object>? LastVariables { get; private set; }

            public Task<PipelineStepExecutionResult> Execute(PipelineStep step, PipelineExecutionContext context, CancellationToken cancellationToken = default)
            {
                ExecutedSteps.Add(step.Name);
                LastVariables = new Dictionary<string, object>(context.StepVariables);
                return Task.FromResult(handler(step));
            }
        }

        private static PipelineStepExecutionResult Failure(bool transient) => new()
        {
            Success = false,
            Error = transient ? "HTTP 503: unavailable" : "HTTP 422: invalid",
            StatusCode = transient ? 503 : 422,
            IsTransient = transient
        };

        private static async Task<(long connectorId, long pipelineId)> SeedPipeline(IServiceProvider serviceProvider, int maxAttempts, bool withErrorStep = false)
        {
            (long connectorId, long pipelineId) = await SeedJavaScriptPipeline(serviceProvider, "result.value = {};");
            DbContext dbContext = serviceProvider.GetRequiredService<DbContext>();

            Pipeline pipeline = await dbContext.Set<Pipeline>().AsTracking().FirstAsync(item => item.Id == pipelineId);
            pipeline.SetMaxAttempts(maxAttempts);

            if (withErrorStep)
            {
                JavaScriptFunction function = await dbContext.Set<JavaScriptFunction>().FirstAsync();
                PipelineStep errorStep = new(pipelineId, 2, "On Error", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, javaScriptFunctionId: function.Id, runOnError: true);
                errorStep.SetCreatedAt(DateTimeOffset.UtcNow);
                dbContext.Add(errorStep);
            }

            await dbContext.SaveChangesAsync();
            return (connectorId, pipelineId);
        }

        private static async Task<ProcessingQueue> Enqueue(IServiceProvider serviceProvider, long connectorId, long pipelineId, string? idempotencyKey = null)
        {
            DbContext dbContext = serviceProvider.GetRequiredService<DbContext>();
            ProcessingQueue item = new(connectorId, pipelineId, 1, ProcessingStatus.Pending, "{}", idempotencyKey: idempotencyKey);
            item.SetCreatedAt(DateTimeOffset.UtcNow);
            dbContext.Add(item);
            await dbContext.SaveChangesAsync();
            return item;
        }

        private static ExecutionEngineService CreateEngine(IServiceProvider serviceProvider, ScriptedStepExecutor executor)
        {
            return ActivatorUtilities.CreateInstance<ExecutionEngineService>(serviceProvider, executor);
        }

        private static async Task<ProcessingQueue> LoadQueueItem(IServiceProvider serviceProvider, long id)
        {
            DbContext dbContext = serviceProvider.GetRequiredService<DbContext>();
            return await dbContext.Set<ProcessingQueue>().AsNoTracking().FirstAsync(item => item.Id == id);
        }

        [Test]
        public async Task Transient_failure_with_max_attempts_3_reschedules_twice_then_fails()
        {
            await InScopeAsync(async serviceProvider =>
            {
                (long connectorId, long pipelineId) = await SeedPipeline(serviceProvider, maxAttempts: 3);
                ProcessingQueue item = await Enqueue(serviceProvider, connectorId, pipelineId);
                ScriptedStepExecutor executor = new(_ => Failure(transient: true));
                ExecutionEngineService engine = CreateEngine(serviceProvider, executor);

                await engine.ExecutePipeline(connectorId, pipelineId, "{}", ExecutionType.Pipeline, item);
                ProcessingQueue afterFirst = await LoadQueueItem(serviceProvider, item.Id);
                afterFirst.Status.Should().Be(ProcessingStatus.Pending);
                afterFirst.Attempts.Should().Be(1);
                afterFirst.ScheduledAt.Should().BeCloseTo(DateTimeOffset.UtcNow.AddSeconds(30), TimeSpan.FromSeconds(10));

                await engine.ExecutePipeline(connectorId, pipelineId, "{}", ExecutionType.Pipeline, item);
                ProcessingQueue afterSecond = await LoadQueueItem(serviceProvider, item.Id);
                afterSecond.Status.Should().Be(ProcessingStatus.Pending);
                afterSecond.Attempts.Should().Be(2);
                afterSecond.ScheduledAt.Should().BeCloseTo(DateTimeOffset.UtcNow.AddSeconds(60), TimeSpan.FromSeconds(10));

                await engine.ExecutePipeline(connectorId, pipelineId, "{}", ExecutionType.Pipeline, item);
                ProcessingQueue afterThird = await LoadQueueItem(serviceProvider, item.Id);
                afterThird.Status.Should().Be(ProcessingStatus.Error);
                afterThird.Attempts.Should().Be(2);

                DbContext dbContext = serviceProvider.GetRequiredService<DbContext>();
                List<Execution> executions = await dbContext.Set<Execution>().AsNoTracking().ToListAsync();
                executions.Should().HaveCount(3);
                executions.Should().OnlyContain(execution => execution.Status == ExecutionStatus.Error);
            });
        }

        [Test]
        public async Task Transient_failure_with_default_max_attempts_fails_immediately()
        {
            await InScopeAsync(async serviceProvider =>
            {
                (long connectorId, long pipelineId) = await SeedPipeline(serviceProvider, maxAttempts: 1, withErrorStep: true);
                ProcessingQueue item = await Enqueue(serviceProvider, connectorId, pipelineId);
                ScriptedStepExecutor executor = new(_ => Failure(transient: true));
                ExecutionEngineService engine = CreateEngine(serviceProvider, executor);

                await engine.ExecutePipeline(connectorId, pipelineId, "{}", ExecutionType.Pipeline, item);

                ProcessingQueue stored = await LoadQueueItem(serviceProvider, item.Id);
                stored.Status.Should().Be(ProcessingStatus.Error);
                stored.Attempts.Should().Be(0);
                executor.ExecutedSteps.Should().Contain("On Error");
            });
        }

        [Test]
        public async Task Non_transient_failure_never_reschedules()
        {
            await InScopeAsync(async serviceProvider =>
            {
                (long connectorId, long pipelineId) = await SeedPipeline(serviceProvider, maxAttempts: 3, withErrorStep: true);
                ProcessingQueue item = await Enqueue(serviceProvider, connectorId, pipelineId);
                ScriptedStepExecutor executor = new(_ => Failure(transient: false));
                ExecutionEngineService engine = CreateEngine(serviceProvider, executor);

                await engine.ExecutePipeline(connectorId, pipelineId, "{}", ExecutionType.Pipeline, item);

                ProcessingQueue stored = await LoadQueueItem(serviceProvider, item.Id);
                stored.Status.Should().Be(ProcessingStatus.Error);
                stored.Attempts.Should().Be(0);
                executor.ExecutedSteps.Should().Contain("On Error");
            });
        }

        [Test]
        public async Task Manual_execution_without_queue_item_never_reschedules()
        {
            await InScopeAsync(async serviceProvider =>
            {
                (long connectorId, long pipelineId) = await SeedPipeline(serviceProvider, maxAttempts: 3, withErrorStep: true);
                ScriptedStepExecutor executor = new(_ => Failure(transient: true));
                ExecutionEngineService engine = CreateEngine(serviceProvider, executor);

                Execution execution = await engine.ExecutePipeline(connectorId, pipelineId, "{}", ExecutionType.Manual);

                execution.Status.Should().Be(ExecutionStatus.Error);
                executor.ExecutedSteps.Should().Contain("On Error");
                DbContext dbContext = serviceProvider.GetRequiredService<DbContext>();
                (await dbContext.Set<ProcessingQueue>().CountAsync()).Should().Be(0);
            });
        }

        [Test]
        public async Task RunOnError_step_runs_only_on_final_failure()
        {
            await InScopeAsync(async serviceProvider =>
            {
                (long connectorId, long pipelineId) = await SeedPipeline(serviceProvider, maxAttempts: 2, withErrorStep: true);
                ProcessingQueue item = await Enqueue(serviceProvider, connectorId, pipelineId);
                ScriptedStepExecutor executor = new(step => step.RunOnError ? new PipelineStepExecutionResult { Success = true } : Failure(transient: true));
                ExecutionEngineService engine = CreateEngine(serviceProvider, executor);

                await engine.ExecutePipeline(connectorId, pipelineId, "{}", ExecutionType.Pipeline, item);
                executor.ExecutedSteps.Should().NotContain("On Error");

                await engine.ExecutePipeline(connectorId, pipelineId, "{}", ExecutionType.Pipeline, item);
                executor.ExecutedSteps.Should().Contain("On Error");
                (await LoadQueueItem(serviceProvider, item.Id)).Status.Should().Be(ProcessingStatus.Error);
            });
        }

        [Test]
        public async Task Queue_item_exposes_idempotency_key_variable()
        {
            await InScopeAsync(async serviceProvider =>
            {
                (long connectorId, long pipelineId) = await SeedPipeline(serviceProvider, maxAttempts: 1);
                ProcessingQueue withKey = await Enqueue(serviceProvider, connectorId, pipelineId, "k-123");
                ProcessingQueue withoutKey = await Enqueue(serviceProvider, connectorId, pipelineId);
                ScriptedStepExecutor executor = new(_ => new PipelineStepExecutionResult { Success = true });
                ExecutionEngineService engine = CreateEngine(serviceProvider, executor);

                await engine.ExecutePipeline(connectorId, pipelineId, "{}", ExecutionType.Pipeline, withKey);
                executor.LastVariables!["idempotencyKey"].Should().Be("k-123");

                await engine.ExecutePipeline(connectorId, pipelineId, "{}", ExecutionType.Pipeline, withoutKey);
                executor.LastVariables!["idempotencyKey"].Should().Be($"q{withoutKey.Id}");
            });
        }

        [Test]
        public async Task Payload_idempotency_key_is_not_shadowed_by_queue_variable()
        {
            await InScopeAsync(async serviceProvider =>
            {
                (long connectorId, long pipelineId) = await SeedPipeline(serviceProvider, maxAttempts: 1);
                ProcessingQueue item = await Enqueue(serviceProvider, connectorId, pipelineId);
                ScriptedStepExecutor executor = new(_ => new PipelineStepExecutionResult { Success = true });
                ExecutionEngineService engine = CreateEngine(serviceProvider, executor);

                await engine.ExecutePipeline(connectorId, pipelineId, "{\"idempotencyKey\":\"from-payload\"}", ExecutionType.Pipeline, item);

                executor.LastVariables.Should().NotContainKey("idempotencyKey");
            });
        }

        [Test]
        public async Task ProcessItem_reschedules_and_pending_query_waits_for_backoff()
        {
            await InScopeAsync(async serviceProvider =>
            {
                (long connectorId, long pipelineId) = await SeedPipeline(serviceProvider, maxAttempts: 2);
                ProcessingQueue item = await Enqueue(serviceProvider, connectorId, pipelineId);
                DbContext dbContext = serviceProvider.GetRequiredService<DbContext>();
                ExecutionEngineService engine = CreateEngine(serviceProvider, new ScriptedStepExecutor(_ => Failure(transient: true)));
                QueueProcessorService processor = new(dbContext, engine);

                await processor.ProcessItem(item.Id);

                ProcessingQueue rescheduled = await LoadQueueItem(serviceProvider, item.Id);
                rescheduled.Status.Should().Be(ProcessingStatus.Pending);
                rescheduled.Attempts.Should().Be(1);
                (await processor.GetPendingToProcess(10)).Should().BeEmpty();

                DateTimeOffset past = DateTimeOffset.UtcNow.AddSeconds(-1);
                await dbContext.Set<ProcessingQueue>().Where(current => current.Id == item.Id)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(current => current.ScheduledAt, (DateTimeOffset?)past));
                (await processor.GetPendingToProcess(10)).Should().ContainSingle(current => current.Id == item.Id);

                await processor.ProcessItem(item.Id);

                ProcessingQueue final = await LoadQueueItem(serviceProvider, item.Id);
                final.Status.Should().Be(ProcessingStatus.Error);
                final.Attempts.Should().Be(1);
            });
        }
    }
}
