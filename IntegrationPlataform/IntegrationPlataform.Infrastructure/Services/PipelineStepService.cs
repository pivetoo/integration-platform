using Archon.Infrastructure.Services;
using IntegrationPlataform.Application.Requests.PipelineSteps;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using IntegrationPlataform.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Infrastructure.Services
{
    public sealed class PipelineStepService : CrudService<PipelineStep>, IPipelineStepService
    {
        public PipelineStepService(DbContext dbContext) : base(dbContext)
        {
        }

        public async Task<PipelineStep> CreatePipelineStep(CreatePipelineStepRequest request, CancellationToken cancellationToken = default)
        {
            await EnsurePipelineExists(request.PipelineId, cancellationToken);
            await ValidateLinkedResource(request.Type, request.ApiCallId, request.JavaScriptFunctionId, request.DatabaseScriptId, cancellationToken);
            await EnsureUniqueOrder(request.PipelineId, request.Order, null, cancellationToken);

            PipelineStep step = new(
                request.PipelineId,
                request.Order,
                request.Name,
                request.Type,
                request.ErrorAction,
                request.ApiCallId,
                request.JavaScriptFunctionId,
                request.DatabaseScriptId,
                request.IgnoreOnResponse);

            bool success = await Insert(cancellationToken, step);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return step;
        }

        public async Task<PipelineStep> UpdatePipelineStep(long id, UpdatePipelineStepRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException("Route id does not match body id.");
            }

            PipelineStep? step = await DbContext.Set<PipelineStep>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (step is null)
            {
                throw new InvalidOperationException("Pipeline step not found.");
            }

            await ValidateLinkedResource(request.Type, request.ApiCallId, request.JavaScriptFunctionId, request.DatabaseScriptId, cancellationToken);
            await EnsureUniqueOrder(step.PipelineId, request.Order, id, cancellationToken);

            step.Update(
                request.Order,
                request.Name,
                request.Type,
                request.ErrorAction,
                request.ApiCallId,
                request.JavaScriptFunctionId,
                request.DatabaseScriptId,
                request.IsActive,
                request.IgnoreOnResponse);

            PipelineStep? result = await Update(step, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return result;
        }

        private async Task EnsurePipelineExists(long pipelineId, CancellationToken cancellationToken)
        {
            bool exists = await DbContext.Set<Pipeline>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == pipelineId, cancellationToken);

            if (!exists)
            {
                throw new InvalidOperationException("Pipeline not found.");
            }
        }

        private async Task EnsureUniqueOrder(long pipelineId, int order, long? currentId, CancellationToken cancellationToken)
        {
            bool exists = await DbContext.Set<PipelineStep>()
                .AsNoTracking()
                .AnyAsync(
                    item => item.PipelineId == pipelineId &&
                    item.Order == order &&
                    (!currentId.HasValue || item.Id != currentId.Value),
                    cancellationToken);

            if (exists)
            {
                throw new InvalidOperationException("There is already a pipeline step with this order.");
            }
        }

        private async Task ValidateLinkedResource(
            PipelineStepType type,
            long? apiCallId,
            long? javaScriptFunctionId,
            long? databaseScriptId,
            CancellationToken cancellationToken)
        {
            switch (type)
            {
                case PipelineStepType.HttpRequest:
                    if (!apiCallId.HasValue)
                    {
                        throw new InvalidOperationException("ApiCallId is required for HTTP request steps.");
                    }

                    await EnsureEntityExists<ApiCall>(apiCallId.Value, "Api call not found.", cancellationToken);
                    break;

                case PipelineStepType.JavaScriptFunction:
                    if (!javaScriptFunctionId.HasValue)
                    {
                        throw new InvalidOperationException("JavaScriptFunctionId is required for JavaScript steps.");
                    }

                    await EnsureEntityExists<JavaScriptFunction>(javaScriptFunctionId.Value, "JavaScript function not found.", cancellationToken);
                    break;

                case PipelineStepType.ExecuteScript:
                    if (!databaseScriptId.HasValue)
                    {
                        throw new InvalidOperationException("DatabaseScriptId is required for script execution steps.");
                    }

                    await EnsureEntityExists<DatabaseScript>(databaseScriptId.Value, "Database script not found.", cancellationToken);
                    break;
            }
        }

        private async Task EnsureEntityExists<TEntity>(long id, string message, CancellationToken cancellationToken) where TEntity : class
        {
            bool exists = await DbContext.Set<TEntity>().AsNoTracking().AnyAsync(item => EF.Property<long>(item, "Id") == id, cancellationToken);
            if (!exists)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
