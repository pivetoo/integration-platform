using IntegrationPlatform.Application.Models;
using IntegrationPlatform.Application.Localization;
using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using IntegrationPlatform.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text.Json;

namespace IntegrationPlatform.Infrastructure.Services.ExecutionEngine
{
    public sealed class ExecutionEngineService : IExecutionEngineService
    {
        private static readonly ConcurrentDictionary<string, DebugSessionState> DebugSessions = [];
        private static readonly HashSet<string> EnvelopeKeys = new(StringComparer.OrdinalIgnoreCase) { "result", "data", "response", "value" };

        private readonly DbContext dbContext;
        private readonly IStepExecutorService stepExecutorService;
        private readonly IServiceCallbackDispatcher serviceCallbackDispatcher;
        private readonly IStringLocalizer<IntegrationPlatformResource> Localizer;

        public ExecutionEngineService(DbContext dbContext, IStepExecutorService stepExecutorService, IServiceCallbackDispatcher serviceCallbackDispatcher, IStringLocalizer<IntegrationPlatformResource> localizer)
        {
            this.dbContext = dbContext;
            this.stepExecutorService = stepExecutorService;
            this.serviceCallbackDispatcher = serviceCallbackDispatcher;
            Localizer = localizer;
        }

        public async Task<Execution> ExecutePipeline(long connectorId, long pipelineId, string? inputData, ExecutionType type, ProcessingQueue? queueItem = null, long? initialStepId = null, CancellationToken cancellationToken = default)
        {
            Connector connector = await GetConnector(connectorId, cancellationToken);
            Pipeline pipeline = await GetPipeline(pipelineId, cancellationToken);
            ProcessingQueue? trackedQueueItem = queueItem is not null
                ? await dbContext.Set<ProcessingQueue>().AsTracking().FirstOrDefaultAsync(item => item.Id == queueItem.Id, cancellationToken)
                : null;

            Execution execution = new(type, connector.Id, ExecutionStatus.Running, DateTimeOffset.UtcNow, pipeline.Id, trackedQueueItem?.Id, inputData);
            execution.SetCreatedAt(DateTimeOffset.UtcNow);
            dbContext.Set<Execution>().Add(execution);
            await dbContext.SaveChangesAsync(cancellationToken);

            PipelineExecutionContext context = BuildContext(connector, pipeline, execution, inputData);
            List<ExecutionLog> logs = [];
            Dictionary<string, object?> stepOutputs = [];
            int nextOutputIndex = 1;
            Stopwatch stopwatch = Stopwatch.StartNew();
            bool hasFailure = false;
            bool stopped = false;

            List<PipelineStep> activeSteps = pipeline.Steps
                .Where(step => step.IsActive)
                .OrderBy(step => step.Order)
                .ToList();

            if (initialStepId.HasValue)
            {
                PipelineStep? initialStep = activeSteps.FirstOrDefault(step => step.Id == initialStepId.Value);
                if (initialStep is null)
                {
                    throw new KeyNotFoundException(Localizer["execution.debug.initialStep.notFound"]);
                }

                activeSteps = activeSteps
                    .Where(step => step.Order >= initialStep.Order)
                    .OrderBy(step => step.Order)
                    .ToList();

                AddLog(logs, execution, null, LogLevelType.Info, Localizer["execution.log.debug.startedFromStep", initialStep.Name]);
            }

            AddLog(logs, execution, null, LogLevelType.Info, Localizer["execution.log.pipeline.starting", pipeline.Name, activeSteps.Count]);

            foreach (PipelineStep step in activeSteps)
            {
                if (stopped)
                {
                    break;
                }

                AddLog(logs, execution, step, LogLevelType.Info, Localizer["execution.log.step.executing", step.Name, step.Type]);

                PipelineStepExecutionResult result = await stepExecutorService.Execute(step, context, cancellationToken);
                AddResultLog(logs, execution, step, result);

                if (!step.IgnoreOnResponse)
                {
                    stepOutputs[nextOutputIndex.ToString()] = CreateStepOutput(step, result);
                    nextOutputIndex++;
                }

                ExtractStepVariables(result.ResponseBody, context);

                if (!result.Success)
                {
                    hasFailure = true;
                    context.LastError = result.Error;

                    if (step.ErrorAction == ErrorAction.Stop)
                    {
                        context.HasError = true;
                        stopped = true;
                        AddLog(logs, execution, step, LogLevelType.Error, Localizer["execution.log.pipeline.stoppedDueToError", step.Name, result.Error ?? string.Empty]);
                    }
                    else
                    {
                        context.HasError = true;
                        AddLog(logs, execution, step, LogLevelType.Warning, Localizer["execution.log.step.continueAfterError", step.Name, result.Error ?? string.Empty]);
                    }
                }
            }

            stopwatch.Stop();

            ExecutionStatus finalStatus = stopped
                ? ExecutionStatus.Error
                : hasFailure
                    ? ExecutionStatus.Partial
                    : ExecutionStatus.Success;

            execution.Complete(finalStatus, DateTimeOffset.UtcNow, SerializeSafely(stepOutputs), context.LastError);
            AddLog(logs, execution, null, LogLevelType.Info, Localizer["execution.log.pipeline.finished", finalStatus, stopwatch.ElapsedMilliseconds]);

            await PersistResults(execution, logs, trackedQueueItem, finalStatus, context.LastError, cancellationToken);
            return execution;
        }

        public async Task<DebugSessionState> StartDebugPipeline(long connectorId, long pipelineId, string? inputData, long? initialStepId = null, CancellationToken cancellationToken = default)
        {
            Connector connector = await GetConnector(connectorId, cancellationToken);
            Pipeline pipeline = await GetPipeline(pipelineId, cancellationToken);

            Execution execution = new(ExecutionType.Manual, connector.Id, ExecutionStatus.Running, DateTimeOffset.UtcNow, pipeline.Id, null, inputData);
            execution.SetCreatedAt(DateTimeOffset.UtcNow);
            dbContext.Set<Execution>().Add(execution);
            await dbContext.SaveChangesAsync(cancellationToken);

            PipelineExecutionContext context = BuildContext(connector, pipeline, execution, inputData);
            List<PipelineStep> activeSteps = pipeline.Steps
                .Where(step => step.IsActive)
                .OrderBy(step => step.Order)
                .ToList();

            if (initialStepId.HasValue)
            {
                PipelineStep? initialStep = activeSteps.FirstOrDefault(step => step.Id == initialStepId.Value);
                if (initialStep is null)
                {
                    throw new KeyNotFoundException(Localizer["execution.debug.initialStep.notFound"]);
                }

                activeSteps = activeSteps
                    .Where(step => step.Order >= initialStep.Order)
                    .OrderBy(step => step.Order)
                    .ToList();
            }

            List<ExecutionLog> logs = [];
            if (initialStepId.HasValue && activeSteps.Count > 0)
            {
                AddLog(logs, execution, null, LogLevelType.Info, Localizer["execution.log.debug.startedFromStep", activeSteps[0].Name]);
            }

            AddLog(logs, execution, null, LogLevelType.Info, Localizer["execution.log.debug.started", pipeline.Name, activeSteps.Count]);
            await InsertLogs(logs, cancellationToken);

            DebugSessionState state = new()
            {
                SessionId = Guid.NewGuid().ToString("N"),
                Execution = execution,
                Connector = connector,
                Pipeline = pipeline,
                Context = context,
                ActiveSteps = activeSteps,
                CurrentIndex = 0,
                HasFailure = false,
                Stopped = false,
                Stopwatch = Stopwatch.StartNew()
            };

            DebugSessions[state.SessionId] = state;
            return state;
        }

        public async Task<ExecuteNextDebugStepResult> ExecuteNextDebugStep(string debugSessionId, CancellationToken cancellationToken = default)
        {
            if (!DebugSessions.TryGetValue(debugSessionId, out DebugSessionState? state))
            {
                throw new KeyNotFoundException(Localizer["execution.debug.session.notFound"]);
            }

            if (state.Stopped || state.CurrentIndex >= state.ActiveSteps.Count)
            {
                return new ExecuteNextDebugStepResult
                {
                    DebugSessionId = state.SessionId,
                    ExecutionId = state.Execution.Id,
                    ExecutedStep = false,
                    FinishedFlow = true,
                    RemainingSteps = 0,
                    Message = Localizer["execution.debug.noMoreSteps"].Value
                };
            }

            PipelineStep step = state.ActiveSteps[state.CurrentIndex];
            List<ExecutionLog> logs = [];

            AddLog(logs, state.Execution, step, LogLevelType.Info, Localizer["execution.log.step.executing", step.Name, step.Type]);

            PipelineStepExecutionResult result = await stepExecutorService.Execute(step, state.Context, cancellationToken);
            AddResultLog(logs, state.Execution, step, result);

            if (!step.IgnoreOnResponse)
            {
                state.StepOutputs[state.NextOutputIndex.ToString()] = CreateStepOutput(step, result);
                state.NextOutputIndex++;
            }

            ExtractStepVariables(result.ResponseBody, state.Context);

            if (!result.Success)
            {
                state.HasFailure = true;
                state.Context.LastError = result.Error;

                if (step.ErrorAction == ErrorAction.Stop)
                {
                    state.Context.HasError = true;
                    state.Stopped = true;
                    AddLog(logs, state.Execution, step, LogLevelType.Error, Localizer["execution.log.pipeline.stoppedDueToError", step.Name, result.Error ?? string.Empty]);
                }
                else
                {
                    state.Context.HasError = true;
                    AddLog(logs, state.Execution, step, LogLevelType.Warning, Localizer["execution.log.step.continueAfterError", step.Name, result.Error ?? string.Empty]);
                }
            }

            await InsertLogs(logs, cancellationToken);

            state.CurrentIndex++;
            bool finishedFlow = state.Stopped || state.CurrentIndex >= state.ActiveSteps.Count;
            PipelineStep? nextStep = !finishedFlow ? state.ActiveSteps[state.CurrentIndex] : null;

            return new ExecuteNextDebugStepResult
            {
                DebugSessionId = state.SessionId,
                ExecutionId = state.Execution.Id,
                ExecutedStep = true,
                ExecutedStepId = step.Id,
                ExecutedStepName = step.Name,
                Success = result.Success,
                InterruptedByError = state.Stopped,
                FinishedFlow = finishedFlow,
                RemainingSteps = finishedFlow ? 0 : state.ActiveSteps.Count - state.CurrentIndex,
                NextStepId = nextStep?.Id,
                NextStepName = nextStep?.Name
            };
        }

        public async Task<Execution> FinishDebugPipeline(string debugSessionId, CancellationToken cancellationToken = default)
        {
            if (!DebugSessions.TryGetValue(debugSessionId, out DebugSessionState? state))
            {
                throw new KeyNotFoundException(Localizer["execution.debug.session.notFound"]);
            }

            state.Stopwatch.Stop();

            ExecutionStatus finalStatus = state.Stopped
                ? ExecutionStatus.Error
                : state.HasFailure
                    ? ExecutionStatus.Partial
                    : ExecutionStatus.Success;

            state.Execution.Complete(finalStatus, DateTimeOffset.UtcNow, SerializeSafely(state.StepOutputs), state.Context.LastError);

            List<ExecutionLog> logs =
            [
                CreateLog(state.Execution, null, LogLevelType.Info, Localizer["execution.log.debug.finished", finalStatus, state.Stopwatch.ElapsedMilliseconds])
            ];

            await PersistResults(state.Execution, logs, null, finalStatus, state.Context.LastError, CancellationToken.None);
            DebugSessions.TryRemove(debugSessionId, out _);
            return state.Execution;
        }

        public async Task<Execution> ExecutePipelineByIdentifier(string integrationIdentifier, string pipelineIdentifier, Dictionary<string, object> inputData, ExecutionType type, CancellationToken cancellationToken = default)
        {
            Integration? integration = await (
                from item in dbContext.Set<Integration>().AsNoTracking()
                where item.Identifier == integrationIdentifier
                select item)
                .FirstOrDefaultAsync(cancellationToken);

            if (integration is null)
            {
                throw new KeyNotFoundException(Localizer["execution.integration.notFoundByIdentifier", integrationIdentifier]);
            }

            Connector? connector = await (
                from item in dbContext.Set<Connector>().AsNoTracking()
                where item.IntegrationId == integration.Id
                orderby item.Id
                select item)
                .FirstOrDefaultAsync(cancellationToken);

            if (connector is null)
            {
                throw new KeyNotFoundException(Localizer["execution.connector.notFoundByIntegration", integrationIdentifier]);
            }

            Pipeline? pipeline = await (
                from item in dbContext.Set<Pipeline>().AsNoTracking()
                where item.IntegrationId == integration.Id && item.Identifier == pipelineIdentifier
                select item)
                .FirstOrDefaultAsync(cancellationToken);

            if (pipeline is null)
            {
                throw new KeyNotFoundException(Localizer["execution.pipeline.notFoundByIntegration", pipelineIdentifier, integrationIdentifier]);
            }

            string inputDataJson = JsonSerializer.Serialize(inputData);
            return await ExecutePipeline(connector.Id, pipeline.Id, inputDataJson, type, null, null, cancellationToken);
        }

        public async Task<Execution> ExecuteWebhook(string webhookToken, string rawBody, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(webhookToken);

            Connector? connector = await dbContext.Set<Connector>()
                .AsNoTracking()
                .Include(item => item.Integration)
                .FirstOrDefaultAsync(item => item.WebhookToken == webhookToken, cancellationToken);

            if (connector is null)
            {
                throw new KeyNotFoundException(Localizer["webhook.token.notFound"]);
            }

            if (!connector.IsActive)
            {
                throw new InvalidOperationException("webhook.connector.inactive");
            }

            string pipelineIdentifier = $"{connector.Integration.Identifier}-webhook";

            Pipeline? pipeline = await dbContext.Set<Pipeline>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.IntegrationId == connector.IntegrationId && item.Identifier == pipelineIdentifier && item.IsActive, cancellationToken);

            if (pipeline is null)
            {
                throw new KeyNotFoundException(Localizer["webhook.pipeline.notFound", pipelineIdentifier]);
            }

            string normalizedBody = WrapPayloadAsObject(rawBody);

            return await ExecutePipeline(connector.Id, pipeline.Id, normalizedBody, ExecutionType.Webhook, null, null, cancellationToken);
        }

        public async Task<Execution> ExecuteWebhookByIntegration(string integrationIdentifier, string rawBody, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(integrationIdentifier);

            Integration? integration = await dbContext.Set<Integration>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Identifier == integrationIdentifier, cancellationToken);

            if (integration is null)
            {
                throw new KeyNotFoundException(Localizer["execution.integration.notFoundByIdentifier", integrationIdentifier]);
            }

            Connector? connector = await dbContext.Set<Connector>()
                .AsNoTracking()
                .Where(item => item.IntegrationId == integration.Id && item.IsActive)
                .OrderBy(item => item.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (connector is null)
            {
                throw new KeyNotFoundException(Localizer["execution.connector.notFoundByIntegration", integrationIdentifier]);
            }

            string pipelineIdentifier = $"{integration.Identifier}-webhook";

            Pipeline? pipeline = await dbContext.Set<Pipeline>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.IntegrationId == integration.Id && item.Identifier == pipelineIdentifier && item.IsActive, cancellationToken);

            if (pipeline is null)
            {
                throw new KeyNotFoundException(Localizer["webhook.pipeline.notFound", pipelineIdentifier]);
            }

            string normalizedBody = WrapPayloadAsObject(rawBody);

            return await ExecutePipeline(connector.Id, pipeline.Id, normalizedBody, ExecutionType.Webhook, null, null, cancellationToken);
        }

        private static string WrapPayloadAsObject(string rawBody)
        {
            if (string.IsNullOrWhiteSpace(rawBody))
            {
                return "{}";
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(rawBody);
                if (document.RootElement.ValueKind == JsonValueKind.Object)
                {
                    return rawBody;
                }
                return JsonSerializer.Serialize(new { raw = JsonSerializer.Deserialize<object>(rawBody) });
            }
            catch
            {
                return JsonSerializer.Serialize(new { raw = rawBody });
            }
        }

        private async Task<Connector> GetConnector(long connectorId, CancellationToken cancellationToken)
        {
            Connector? connector = await dbContext.Set<Connector>()
                .AsNoTracking()
                .Include(item => item.AttributeValues)
                    .ThenInclude(item => item.IntegrationAttribute)
                .FirstOrDefaultAsync(item => item.Id == connectorId, cancellationToken);

            return connector ?? throw new KeyNotFoundException(Localizer["connector.notFound"]);
        }

        private async Task<Pipeline> GetPipeline(long pipelineId, CancellationToken cancellationToken)
        {
            Pipeline? pipeline = await dbContext.Set<Pipeline>()
                .AsNoTracking()
                .Include(item => item.Steps)
                    .ThenInclude(item => item.ApiCall)
                .Include(item => item.Steps)
                    .ThenInclude(item => item.JavaScriptFunction)
                .Include(item => item.Steps)
                    .ThenInclude(item => item.DatabaseScript)
                        .ThenInclude(item => item!.DatabaseConnection)
                .FirstOrDefaultAsync(item => item.Id == pipelineId, cancellationToken);

            return pipeline ?? throw new KeyNotFoundException(Localizer["pipeline.notFound"]);
        }

        private PipelineExecutionContext BuildContext(Connector connector, Pipeline pipeline, Execution execution, string? inputData)
        {
            PipelineExecutionContext context = new()
            {
                Connector = connector,
                Pipeline = pipeline,
                Execution = execution
            };

            foreach (ConnectorAttributeValue attribute in connector.AttributeValues)
            {
                context.ConnectorAttributes[attribute.IntegrationAttribute.Field] = attribute.Value;
            }

            if (!string.IsNullOrWhiteSpace(inputData))
            {
                try
                {
                    Dictionary<string, object>? payload = JsonSerializer.Deserialize<Dictionary<string, object>>(inputData);
                    if (payload is not null)
                    {
                        context.PayloadData = payload;
                    }
                }
                catch (Exception exception)
                {
                    throw new ValidationException(Localizer["execution.inputData.invalid", exception.Message]);
                }
            }

            return context;
        }

        private async Task PersistResults(Execution execution, List<ExecutionLog> logs, ProcessingQueue? queueItem, ExecutionStatus finalStatus, string? lastError, CancellationToken cancellationToken)
        {
            Execution trackedExecution = await dbContext.Set<Execution>()
                .AsTracking()
                .FirstAsync(item => item.Id == execution.Id, CancellationToken.None);

            trackedExecution.UpdateInput(execution.InputData);

            if (execution.Status == ExecutionStatus.Running)
            {
                trackedExecution.MarkAsRunning();
            }
            else
            {
                trackedExecution.Complete(
                    execution.Status,
                    execution.FinishedAt ?? DateTimeOffset.UtcNow,
                    execution.OutputData,
                    execution.Errors);
            }

            trackedExecution.SetCreatedAt(execution.CreatedAt);
            trackedExecution.SetUpdatedAt(DateTimeOffset.UtcNow);

            if (queueItem is not null)
            {
                if (finalStatus == ExecutionStatus.Error)
                {
                    queueItem.Fail(lastError ?? Localizer["execution.unknownError"].Value, DateTimeOffset.UtcNow);
                }
                else
                {
                    queueItem.Complete(DateTimeOffset.UtcNow);
                }

                queueItem.SetUpdatedAt(DateTimeOffset.UtcNow);
            }

            dbContext.Set<ExecutionLog>().AddRange(logs);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            if (finalStatus == ExecutionStatus.Success)
            {
                ServiceCallbackResult callback = await serviceCallbackDispatcher.DispatchAsync(trackedExecution, cancellationToken);
                if (callback.Attempted)
                {
                    LogLevelType level = callback.Success ? LogLevelType.Info : LogLevelType.Warning;
                    string message = callback.Success
                        ? Localizer["execution.callback.delivered", callback.Detail ?? string.Empty].Value
                        : Localizer["execution.callback.failed", callback.Detail ?? string.Empty].Value;

                    ExecutionLog callbackLog = CreateLog(trackedExecution, null, level, message);
                    callbackLog.SetCreatedAt(DateTimeOffset.UtcNow);
                    dbContext.Set<ExecutionLog>().Add(callbackLog);
                    await dbContext.SaveChangesAsync(CancellationToken.None);
                }
            }
        }

        private async Task InsertLogs(IEnumerable<ExecutionLog> logs, CancellationToken cancellationToken)
        {
            List<ExecutionLog> materializedLogs = logs.ToList();
            if (materializedLogs.Count == 0)
            {
                return;
            }

            foreach (ExecutionLog log in materializedLogs)
            {
                log.SetCreatedAt(DateTimeOffset.UtcNow);
            }

            dbContext.Set<ExecutionLog>().AddRange(materializedLogs);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private static ExecutionLog CreateLog(Execution execution, PipelineStep? step, LogLevelType level, string message)
        {
            ExecutionLog log = new(execution.Id, level, message, step?.Id);
            log.SetCreatedAt(DateTimeOffset.UtcNow);
            return log;
        }

        private static void AddLog(List<ExecutionLog> logs, Execution execution, PipelineStep? step, LogLevelType level, string message)
        {
            logs.Add(CreateLog(execution, step, level, message));
        }

        private void AddResultLog(List<ExecutionLog> logs, Execution execution, PipelineStep step, PipelineStepExecutionResult result)
        {
            ExecutionLog log = new(
                execution.Id,
                result.Success ? LogLevelType.Info : LogLevelType.Error,
                result.Success
                    ? Localizer["execution.log.step.succeeded", step.Name]
                    : Localizer["execution.log.step.failed", step.Name, result.Error ?? string.Empty],
                step.Id,
                request: result.RequestInfo,
                response: result.ResponseBody,
                httpStatusCode: result.StatusCode,
                duration: result.DurationInMilliseconds);

            log.SetCreatedAt(DateTimeOffset.UtcNow);
            logs.Add(log);
        }

        private static Dictionary<string, object?> CreateStepOutput(PipelineStep step, PipelineStepExecutionResult result)
        {
            return new Dictionary<string, object?>
            {
                ["stepId"] = step.Id,
                ["name"] = step.Name,
                ["output"] = ResolveStepOutput(result)
            };
        }

        private static object? ResolveStepOutput(PipelineStepExecutionResult result)
        {
            if (!string.IsNullOrWhiteSpace(result.ResponseBody))
            {
                try
                {
                    return JsonSerializer.Deserialize<object>(result.ResponseBody);
                }
                catch
                {
                    return result.ResponseBody;
                }
            }

            return result.ExtractedResult;
        }

        private static string? SerializeSafely(Dictionary<string, object?> outputs)
        {
            try
            {
                return JsonSerializer.Serialize(outputs);
            }
            catch
            {
                return null;
            }
        }

        private static void ExtractStepVariables(string? responseBody, PipelineExecutionContext context)
        {
            if (string.IsNullOrWhiteSpace(responseBody))
            {
                return;
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(responseBody);
                JsonElement? target = ResolveTargetElement(document.RootElement);

                if (target.HasValue && target.Value.ValueKind == JsonValueKind.Object)
                {
                    foreach (JsonProperty property in target.Value.EnumerateObject())
                    {
                        context.StepVariables[property.Name] = property.Value.ValueKind switch
                        {
                            JsonValueKind.String => property.Value.GetString() ?? string.Empty,
                            JsonValueKind.Number when property.Value.TryGetInt64(out long integerValue) => integerValue,
                            JsonValueKind.Number when property.Value.TryGetDouble(out double doubleValue) => doubleValue,
                            JsonValueKind.True => true,
                            JsonValueKind.False => false,
                            JsonValueKind.Object or JsonValueKind.Array => JsonSerializer.Deserialize<object>(property.Value.GetRawText()) ?? property.Value.GetRawText(),
                            JsonValueKind.Null => string.Empty,
                            _ => property.Value.GetRawText()
                        };
                    }
                }
            }
            catch
            {
            }
        }

        private static JsonElement? ResolveTargetElement(JsonElement root)
        {
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("isBinary", out JsonElement isBinaryElement) &&
                    isBinaryElement.ValueKind == JsonValueKind.True)
                {
                    return null;
                }

                JsonProperty[] properties = root.EnumerateObject().ToArray();

                if (properties.Length == 1 && EnvelopeKeys.Contains(properties[0].Name))
                {
                    JsonElement inner = properties[0].Value;

                    if (inner.ValueKind == JsonValueKind.Object)
                    {
                        return inner;
                    }

                    if (inner.ValueKind == JsonValueKind.Array)
                    {
                        JsonElement[] array = inner.EnumerateArray().ToArray();
                        if (array.Length == 1 && array[0].ValueKind == JsonValueKind.Object)
                        {
                            return array[0];
                        }
                    }

                    return null;
                }

                return root;
            }

            if (root.ValueKind == JsonValueKind.Array)
            {
                JsonElement[] array = root.EnumerateArray().ToArray();
                if (array.Length == 1 && array[0].ValueKind == JsonValueKind.Object)
                {
                    return array[0];
                }
            }

            return null;
        }
    }
}
