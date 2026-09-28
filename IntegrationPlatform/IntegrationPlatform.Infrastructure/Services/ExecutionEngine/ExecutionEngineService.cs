using Archon.Application.MultiTenancy;
using ArchonIntegrationService = Archon.Application.Services.IIntegrationService;
using ArchonIntegration = Archon.Application.Integrations.Integration;
using IntegrationPlatform.Application.Models;
using IntegrationPlatform.Application.Localization;
using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using IntegrationPlatform.Domain.ValueObjects;
using IntegrationPlatform.Infrastructure.BackgroundJobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

        // Nome da integracao Archon (integrations/integrationparameters) que carrega config por tenant
        // injetada pelo IdM no onboarding (ex.: CallbackSecret). Simetrico a integracao "integration-platform"
        // que o AgencyCampaign mantem do seu lado. Seus parametros ficam disponiveis na interpolacao do pipeline.
        private const string TenantConfigIntegrationName = "agency-campaign";

        private readonly DbContext dbContext;
        private readonly IStepExecutorService stepExecutorService;
        private readonly IServiceCallbackDispatcher serviceCallbackDispatcher;
        private readonly ITenantContext tenantContext;
        private readonly ArchonIntegrationService integrationService;
        private readonly IStringLocalizer<IntegrationPlatformResource> Localizer;
        private readonly ILogger<ExecutionEngineService> logger;
        private readonly double queueRetryBaseBackoffSeconds;

        public ExecutionEngineService(DbContext dbContext, IStepExecutorService stepExecutorService, IServiceCallbackDispatcher serviceCallbackDispatcher, ITenantContext tenantContext, ArchonIntegrationService integrationService, IStringLocalizer<IntegrationPlatformResource> localizer, ILogger<ExecutionEngineService> logger, IOptions<BackgroundJobOptions> jobOptions)
        {
            this.dbContext = dbContext;
            this.stepExecutorService = stepExecutorService;
            this.serviceCallbackDispatcher = serviceCallbackDispatcher;
            this.tenantContext = tenantContext;
            this.integrationService = integrationService;
            Localizer = localizer;
            this.logger = logger;
            queueRetryBaseBackoffSeconds = jobOptions.Value.QueueRetryBaseBackoffSeconds;
        }

        public async Task<Execution> ExecutePipeline(long connectorId, long pipelineId, string? inputData, ExecutionType type, ProcessingQueue? queueItem = null, long? initialStepId = null, CancellationToken cancellationToken = default)
        {
            Connector connector = await GetConnector(connectorId, cancellationToken);
            Pipeline pipeline = await GetPipeline(pipelineId, cancellationToken);
            ProcessingQueue? trackedQueueItem = queueItem is not null
                ? await dbContext.Set<ProcessingQueue>().AsTracking().FirstOrDefaultAsync(item => item.Id == queueItem.Id, cancellationToken)
                : null;

            if (!PipelinePayloadParser.TryParse(inputData, out Dictionary<string, object> payloadData))
            {
                throw new ValidationException(Localizer["execution.inputData.invalid", inputData ?? string.Empty]);
            }

            Execution execution = new(type, connector.Id, ExecutionStatus.Running, DateTimeOffset.UtcNow, pipeline.Id, trackedQueueItem?.Id, inputData);
            execution.SetCreatedAt(DateTimeOffset.UtcNow);
            dbContext.Set<Execution>().Add(execution);
            await dbContext.SaveChangesAsync(cancellationToken);

            ArchonIntegration? tenantConfig = await integrationService.GetByNameAsync(TenantConfigIntegrationName, cancellationToken);
            PipelineExecutionContext context = BuildContext(connector, pipeline, execution, payloadData, tenantContext.TenantId, tenantConfig);
            if (trackedQueueItem is not null)
            {
                context.StepVariables["idempotencyKey"] = trackedQueueItem.IdempotencyKey ?? $"q{trackedQueueItem.Id}";
            }

            List<ExecutionLog> logs = [];
            Dictionary<string, object?> stepOutputs = [];
            int nextOutputIndex = 1;
            Stopwatch stopwatch = Stopwatch.StartNew();
            bool hasFailure = false;
            bool stopped = false;
            PipelineStepExecutionResult? stoppingResult = null;

            List<PipelineStep> allActiveSteps = pipeline.Steps
                .Where(step => step.IsActive)
                .OrderBy(step => step.Order)
                .ToList();

            // Steps marcados com RunOnError rodam SOMENTE no passo de tratamento de erro (ex.: callback de
            // falha pro Mainstay). Ficam de fora do fluxo normal.
            List<PipelineStep> errorSteps = allActiveSteps.Where(step => step.RunOnError).ToList();
            List<PipelineStep> activeSteps = allActiveSteps.Where(step => !step.RunOnError).ToList();

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

                StepConditionResult condition = StepConditionEvaluator.Evaluate(step.RunCondition, context);
                if (!condition.ShouldRun)
                {
                    if (condition.Error is null)
                    {
                        AddLog(logs, execution, step, LogLevelType.Info, Localizer["execution.log.step.skippedByCondition", step.Name]);
                        continue;
                    }

                    hasFailure = true;
                    context.HasError = true;
                    context.LastError = Localizer["step.condition.invalid", condition.Error].Value;

                    if (step.ErrorAction == ErrorAction.Stop)
                    {
                        stopped = true;
                        AddLog(logs, execution, step, LogLevelType.Error, Localizer["execution.log.pipeline.stoppedDueToError", step.Name, context.LastError]);
                    }
                    else
                    {
                        AddLog(logs, execution, step, LogLevelType.Warning, Localizer["execution.log.step.continueAfterError", step.Name, context.LastError]);
                    }

                    continue;
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
                        stoppingResult = result;
                        AddLog(logs, execution, step, LogLevelType.Error, Localizer["execution.log.pipeline.stoppedDueToError", step.Name, result.Error ?? string.Empty]);
                    }
                    else
                    {
                        context.HasError = true;
                        AddLog(logs, execution, step, LogLevelType.Warning, Localizer["execution.log.step.continueAfterError", step.Name, result.Error ?? string.Empty]);
                    }
                }
            }

            // Falha transitoria de item da fila com tentativas restantes: reagenda em vez de encerrar, e os
            // steps RunOnError (aviso de falha ao consumidor) ficam para a falha final.
            TimeSpan? retryDelay = null;
            if (stopped && trackedQueueItem is not null && stoppingResult is { IsTransient: true })
            {
                retryDelay = RetryPolicy.NextRetryDelay(trackedQueueItem.Attempts, pipeline.MaxAttempts, queueRetryBaseBackoffSeconds);
            }

            if (stopped && errorSteps.Count > 0 && retryDelay is null)
            {
                // Passo de tratamento de erro: roda os steps RunOnError expondo {{errorMessage}}. Nao altera
                // o finalStatus (a execucao permanece Error) — serve so para notificar/compensar a falha.
                context.StepVariables["errorMessage"] = context.LastError ?? string.Empty;

                foreach (PipelineStep errorStep in errorSteps)
                {
                    // No passo de tratamento de erro uma condicao invalida nao muda o desfecho (a execucao
                    // ja esta em Error); ela apenas pula o step com log de aviso.
                    StepConditionResult errorCondition = StepConditionEvaluator.Evaluate(errorStep.RunCondition, context);
                    if (!errorCondition.ShouldRun)
                    {
                        LogLevelType level = errorCondition.Error is null ? LogLevelType.Info : LogLevelType.Warning;
                        string message = errorCondition.Error is null
                            ? Localizer["execution.log.step.skippedByCondition", errorStep.Name]
                            : Localizer["execution.log.step.continueAfterError", errorStep.Name, Localizer["step.condition.invalid", errorCondition.Error].Value];
                        AddLog(logs, execution, errorStep, level, message);
                        continue;
                    }

                    AddLog(logs, execution, errorStep, LogLevelType.Info, Localizer["execution.log.step.executing", errorStep.Name, errorStep.Type]);

                    PipelineStepExecutionResult errorResult = await stepExecutorService.Execute(errorStep, context, cancellationToken);
                    AddResultLog(logs, execution, errorStep, errorResult);
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

            if (retryDelay is not null)
            {
                AddLog(logs, execution, null, LogLevelType.Info, Localizer["execution.log.queue.retryScheduled", trackedQueueItem!.Attempts + 1, DateTimeOffset.UtcNow + retryDelay.Value, context.LastError ?? string.Empty]);
            }

            await PersistResults(execution, logs, trackedQueueItem, finalStatus, context.LastError, retryDelay, cancellationToken);
            return execution;
        }

        public async Task<DebugSessionState> StartDebugPipeline(long connectorId, long pipelineId, string? inputData, long? initialStepId = null, CancellationToken cancellationToken = default)
        {
            Connector connector = await GetConnector(connectorId, cancellationToken);
            Pipeline pipeline = await GetPipeline(pipelineId, cancellationToken);

            if (!PipelinePayloadParser.TryParse(inputData, out Dictionary<string, object> payloadData))
            {
                throw new ValidationException(Localizer["execution.inputData.invalid", inputData ?? string.Empty]);
            }

            Execution execution = new(ExecutionType.Manual, connector.Id, ExecutionStatus.Running, DateTimeOffset.UtcNow, pipeline.Id, null, inputData);
            execution.SetCreatedAt(DateTimeOffset.UtcNow);
            dbContext.Set<Execution>().Add(execution);
            await dbContext.SaveChangesAsync(cancellationToken);

            ArchonIntegration? tenantConfig = await integrationService.GetByNameAsync(TenantConfigIntegrationName, cancellationToken);
            PipelineExecutionContext context = BuildContext(connector, pipeline, execution, payloadData, tenantContext.TenantId, tenantConfig);
            List<PipelineStep> activeSteps = pipeline.Steps
                .Where(step => step.IsActive && !step.RunOnError)
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

            StepConditionResult condition = StepConditionEvaluator.Evaluate(step.RunCondition, state.Context);
            if (!condition.ShouldRun && condition.Error is null)
            {
                AddLog(logs, state.Execution, step, LogLevelType.Info, Localizer["execution.log.step.skippedByCondition", step.Name]);
                await InsertLogs(logs, cancellationToken);

                state.CurrentIndex++;
                bool finished = state.CurrentIndex >= state.ActiveSteps.Count;
                PipelineStep? next = !finished ? state.ActiveSteps[state.CurrentIndex] : null;

                return new ExecuteNextDebugStepResult
                {
                    DebugSessionId = state.SessionId,
                    ExecutionId = state.Execution.Id,
                    ExecutedStep = false,
                    ExecutedStepId = step.Id,
                    ExecutedStepName = step.Name,
                    Success = true,
                    FinishedFlow = finished,
                    RemainingSteps = finished ? 0 : state.ActiveSteps.Count - state.CurrentIndex,
                    NextStepId = next?.Id,
                    NextStepName = next?.Name,
                    Message = Localizer["execution.log.step.skippedByCondition", step.Name].Value
                };
            }

            if (!condition.ShouldRun)
            {
                state.HasFailure = true;
                state.Context.HasError = true;
                state.Context.LastError = Localizer["step.condition.invalid", condition.Error!].Value;

                if (step.ErrorAction == ErrorAction.Stop)
                {
                    state.Stopped = true;
                    AddLog(logs, state.Execution, step, LogLevelType.Error, Localizer["execution.log.pipeline.stoppedDueToError", step.Name, state.Context.LastError]);
                }
                else
                {
                    AddLog(logs, state.Execution, step, LogLevelType.Warning, Localizer["execution.log.step.continueAfterError", step.Name, state.Context.LastError]);
                }

                await InsertLogs(logs, cancellationToken);

                state.CurrentIndex++;
                bool finishedAfterError = state.Stopped || state.CurrentIndex >= state.ActiveSteps.Count;
                PipelineStep? nextAfterError = !finishedAfterError ? state.ActiveSteps[state.CurrentIndex] : null;

                return new ExecuteNextDebugStepResult
                {
                    DebugSessionId = state.SessionId,
                    ExecutionId = state.Execution.Id,
                    ExecutedStep = false,
                    ExecutedStepId = step.Id,
                    ExecutedStepName = step.Name,
                    Success = false,
                    InterruptedByError = state.Stopped,
                    FinishedFlow = finishedAfterError,
                    RemainingSteps = finishedAfterError ? 0 : state.ActiveSteps.Count - state.CurrentIndex,
                    NextStepId = nextAfterError?.Id,
                    NextStepName = nextAfterError?.Name,
                    Message = state.Context.LastError
                };
            }

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

            await PersistResults(state.Execution, logs, null, finalStatus, state.Context.LastError, null, CancellationToken.None);
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

        public async Task<Execution> ExecuteWebhookByIntegration(string integrationIdentifier, string rawBody, string? webhookContext = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
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

            if (!string.IsNullOrWhiteSpace(webhookContext))
            {
                normalizedBody = InjectWebhookContext(normalizedBody, webhookContext);
            }

            if (headers is { Count: > 0 })
            {
                normalizedBody = InjectWebhookHeaders(normalizedBody, headers);
            }

            return await ExecutePipeline(connector.Id, pipeline.Id, normalizedBody, ExecutionType.Webhook, null, null, cancellationToken);
        }

        // Provedores que nao ecoam identificador proprio no payload (ex.: postback da D4Sign) carregam a
        // correlacao no proprio path do webhook (/api/webhooks/{tenant}/{integracao}/{contexto}); o contexto
        // entra no payload como "webhookContext" para os steps do pipeline.
        internal static string InjectWebhookContext(string normalizedBody, string webhookContext)
        {
            Dictionary<string, object>? fields = JsonSerializer.Deserialize<Dictionary<string, object>>(normalizedBody);
            fields ??= [];
            fields["webhookContext"] = webhookContext;
            return JsonSerializer.Serialize(fields);
        }

        // Headers da requisicao HTTP original (ex.: asaas-access-token) entram no payload como
        // "webhookHeaders" (chaves em minusculo) para os steps do pipeline validarem a origem da
        // notificacao - antes disso o receptor descartava os headers por completo (IP-001).
        internal static string InjectWebhookHeaders(string normalizedBody, Dictionary<string, string> headers)
        {
            Dictionary<string, object>? fields = JsonSerializer.Deserialize<Dictionary<string, object>>(normalizedBody);
            fields ??= [];
            fields["webhookHeaders"] = headers;
            return JsonSerializer.Serialize(fields);
        }

        private string WrapPayloadAsObject(string rawBody)
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
            catch (JsonException exception)
            {
                logger.LogDebug(exception, "Webhook body is not valid JSON; wrapping it as a raw payload.");
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

        private static PipelineExecutionContext BuildContext(Connector connector, Pipeline pipeline, Execution execution, Dictionary<string, object> payloadData, string? tenantId, ArchonIntegration? tenantConfig)
        {
            PipelineExecutionContext context = new()
            {
                Connector = connector,
                Pipeline = pipeline,
                Execution = execution,
                PayloadData = payloadData
            };

            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                context.StepVariables["tenantId"] = tenantId;
            }

            foreach (ConnectorAttributeValue attribute in connector.AttributeValues)
            {
                context.ConnectorAttributes[attribute.IntegrationAttribute.Field] = attribute.Value;

                if (attribute.IntegrationAttribute.IsSensitive)
                {
                    context.SensitiveAttributeFields.Add(attribute.IntegrationAttribute.Field);
                }
            }

            ApplyTenantParameters(context, tenantConfig);

            return context;
        }

        // Mescla os parametros de integracao do tenant (config Archon injetada pelo IdM no onboarding) no
        // contexto de interpolacao, sem sobrescrever atributos do conector ja presentes (o conector e mais
        // especifico). Segredos (issecret) entram em SensitiveAttributeFields para nao vazar no escopo do
        // passo JavaScript do usuario. Ficam disponiveis no template como {{Chave}} (ex.: {{CallbackSecret}}).
        internal static void ApplyTenantParameters(PipelineExecutionContext context, ArchonIntegration? tenantConfig)
        {
            if (tenantConfig is null)
            {
                return;
            }

            foreach (Archon.Application.Integrations.IntegrationParameter parameter in tenantConfig.Parameters)
            {
                if (context.ConnectorAttributes.ContainsKey(parameter.Key))
                {
                    continue;
                }

                context.ConnectorAttributes[parameter.Key] = parameter.Value;

                if (parameter.IsSecret)
                {
                    context.SensitiveAttributeFields.Add(parameter.Key);
                }
            }
        }

        private async Task PersistResults(Execution execution, List<ExecutionLog> logs, ProcessingQueue? queueItem, ExecutionStatus finalStatus, string? lastError, TimeSpan? retryDelay, CancellationToken cancellationToken)
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
                if (finalStatus == ExecutionStatus.Error && retryDelay is not null)
                {
                    queueItem.ScheduleRetry(DateTimeOffset.UtcNow + retryDelay.Value, lastError ?? Localizer["execution.unknownError"].Value);
                }
                else if (finalStatus == ExecutionStatus.Error)
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

            if (finalStatus == ExecutionStatus.Success || finalStatus == ExecutionStatus.Partial)
            {
                // CancellationToken.None de proposito: o enfileiramento do callback faz parte da
                // persistencia critica do resultado (igual aos SaveChanges acima); nao pode ser cancelado
                // a meio, senao a execucao fica Success/Partial sem o CallbackDelivery correspondente.
                ServiceCallbackResult callback = await serviceCallbackDispatcher.DispatchAsync(trackedExecution, CancellationToken.None);
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

        private string? SerializeSafely(Dictionary<string, object?> outputs)
        {
            try
            {
                return JsonSerializer.Serialize(outputs);
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Failed to serialize pipeline output; OutputData will be persisted as null.");
                return null;
            }
        }

        private void ExtractStepVariables(string? responseBody, PipelineExecutionContext context)
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
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Failed to extract step variables from the response body; no variables were propagated to the next step.");
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
