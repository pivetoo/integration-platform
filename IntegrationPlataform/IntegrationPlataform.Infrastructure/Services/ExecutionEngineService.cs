using IntegrationPlataform.Application.Models;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using IntegrationPlataform.Domain.ValueObjects;

namespace IntegrationPlataform.Infrastructure.Services
{
    public sealed class ExecutionEngineService : IExecutionEngineService
    {
        public Task<Execution> ExecutePipeline(long connectorId, long pipelineId, string? inputData, ExecutionType type, ProcessingQueue? queueItem = null, long? initialStepId = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Execution engine migration is still pending.");
        }

        public Task<DebugSessionState> StartDebugPipeline(long connectorId, long pipelineId, string? inputData, long? initialStepId = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Debug pipeline migration is still pending.");
        }

        public Task<object?> ExecuteNextDebugStep(string debugSessionId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Debug step execution migration is still pending.");
        }

        public Task<Execution> FinishDebugPipeline(string debugSessionId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Debug pipeline finalization migration is still pending.");
        }

        public Task<Execution> ExecutePipelineByIdentifier(string integrationIdentifier, string pipelineIdentifier, Dictionary<string, object> inputData, ExecutionType type, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Pipeline execution by identifier migration is still pending.");
        }
    }
}
