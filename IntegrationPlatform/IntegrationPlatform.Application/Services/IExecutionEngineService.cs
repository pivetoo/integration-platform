using IntegrationPlatform.Application.Models;
using IntegrationPlatform.Domain.Entities;
using IntegrationPlatform.Domain.ValueObjects;

namespace IntegrationPlatform.Application.Services
{
    public interface IExecutionEngineService
    {
        Task<Execution> ExecutePipeline(long connectorId, long pipelineId, string? inputData, ExecutionType type, ProcessingQueue? queueItem = null, long? initialStepId = null, CancellationToken cancellationToken = default);

        Task<DebugSessionState> StartDebugPipeline(long connectorId, long pipelineId, string? inputData, long? initialStepId = null, CancellationToken cancellationToken = default);

        Task<ExecuteNextDebugStepResult> ExecuteNextDebugStep(string debugSessionId, CancellationToken cancellationToken = default);

        Task<Execution> FinishDebugPipeline(string debugSessionId, CancellationToken cancellationToken = default);

        Task<Execution> ExecutePipelineByIdentifier(string integrationIdentifier, string pipelineIdentifier, Dictionary<string, object> inputData, ExecutionType type, CancellationToken cancellationToken = default);

        Task<Execution> ExecuteWebhookByIntegration(string integrationIdentifier, string rawBody, string? webhookContext = null, CancellationToken cancellationToken = default);
    }
}
