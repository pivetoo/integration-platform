using IntegrationPlataform.Application.Models;
using IntegrationPlataform.Domain.Entities;

namespace IntegrationPlataform.Application.Services
{
    public interface IStepExecutorService
    {
        Task<PipelineStepExecutionResult> Execute(PipelineStep step, PipelineExecutionContext context, CancellationToken cancellationToken = default);
    }
}
