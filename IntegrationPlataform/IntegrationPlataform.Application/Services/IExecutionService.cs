using Archon.Application.Services;
using IntegrationPlataform.Domain.Entities;
using IntegrationPlataform.Domain.ValueObjects;

namespace IntegrationPlataform.Application.Services
{
    public interface IExecutionService : ICrudService<Execution>
    {
        Task<IReadOnlyCollection<Execution>> GetByConnector(long connectorId, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<Execution>> GetByStatus(ExecutionStatus status, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<Execution>> GetRecent(int take, CancellationToken cancellationToken = default);

        Task<ProcessingQueue> EnqueuePipeline(long connectorId, long pipelineId, string? payload, int priority, CancellationToken cancellationToken = default);
    }
}
