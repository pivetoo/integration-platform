using IntegrationPlataform.Application.Models;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;

namespace IntegrationPlataform.Infrastructure.Services
{
    public sealed class StepExecutorService : IStepExecutorService
    {
        public Task<PipelineStepExecutionResult> Execute(PipelineStep step, PipelineExecutionContext context, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Pipeline step execution migration is still pending.");
        }
    }
}
