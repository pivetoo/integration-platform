using IntegrationPlataform.Application.Models;
using IntegrationPlataform.Domain.Entities;
using IntegrationPlataform.Domain.ValueObjects;

namespace IntegrationPlataform.Application.Services
{
    public interface IExecutionEngineService
    {
        Task<Execution> ExecutePipeline(long connectorId, long pipelineId, string? inputData, ExecutionType type, ProcessingQueue? queueItem = null, long? initialStepId = null, CancellationToken cancellationToken = default);

        Task<DebugSessionState> StartDebugPipeline(long connectorId, long pipelineId, string? inputData, long? initialStepId = null, CancellationToken cancellationToken = default);

        Task<object?> ExecuteNextDebugStep(string debugSessionId, CancellationToken cancellationToken = default);

        Task<Execution> FinishDebugPipeline(string debugSessionId, CancellationToken cancellationToken = default);

        Task<Execution> ExecutePipelineByIdentifier(string integrationIdentifier, string pipelineIdentifier, Dictionary<string, object> inputData, ExecutionType type, CancellationToken cancellationToken = default);
    }
}
