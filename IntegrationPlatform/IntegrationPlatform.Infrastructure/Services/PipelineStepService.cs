using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using IntegrationPlatform.Application.Localization;
using IntegrationPlatform.Application.Requests.PipelineSteps;
using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using IntegrationPlatform.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace IntegrationPlatform.Infrastructure.Services
{
    public sealed class PipelineStepService : CrudService<PipelineStep>, IPipelineStepService
    {
        private readonly IStringLocalizer<IntegrationPlatformResource> Localizer;

        public PipelineStepService(DbContext dbContext, IStringLocalizer<IntegrationPlatformResource> localizer) : base(dbContext)
        {
            Localizer = localizer;
        }

        public async Task<PagedResult<PipelineStep>> GetPipelineSteps(PagedRequest request, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<PipelineStep>()
                .AsNoTracking()
                .Include(item => item.ApiCall)
                .Include(item => item.JavaScriptFunction)
                .Include(item => item.DatabaseScript)
                .OrderBy(item => item.Order)
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<PipelineStep?> GetPipelineStepById(long id, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<PipelineStep>()
                .AsNoTracking()
                .Include(item => item.ApiCall)
                .Include(item => item.JavaScriptFunction)
                .Include(item => item.DatabaseScript)
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<List<PipelineStep>> GetPipelineStepsByPipeline(long pipelineId, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<PipelineStep>()
                .AsNoTracking()
                .Include(item => item.ApiCall)
                .Include(item => item.JavaScriptFunction)
                .Include(item => item.DatabaseScript)
                .Where(item => item.PipelineId == pipelineId)
                .OrderBy(item => item.Order)
                .ToListAsync(cancellationToken);
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
                request.IgnoreOnResponse,
                request.RunOnError);

            bool success = await Insert(cancellationToken, step);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetPipelineStepById(step.Id, cancellationToken) ?? step;
        }

        public async Task<PipelineStep> UpdatePipelineStep(long id, UpdatePipelineStepRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException("request.route.idMismatch");
            }

            PipelineStep? step = await DbContext.Set<PipelineStep>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (step is null)
            {
                throw new InvalidOperationException("pipeline.step.notFound");
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
                request.IgnoreOnResponse,
                request.RunOnError);

            PipelineStep? result = await Update(step, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetPipelineStepById(result.Id, cancellationToken) ?? result;
        }

        private async Task EnsurePipelineExists(long pipelineId, CancellationToken cancellationToken)
        {
            bool exists = await DbContext.Set<Pipeline>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == pipelineId, cancellationToken);

            if (!exists)
            {
                throw new InvalidOperationException("pipeline.notFound");
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
                throw new InvalidOperationException("pipeline.step.order.alreadyExists");
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
                        throw new InvalidOperationException("pipeline.step.apiCallId.required");
                    }

                    await EnsureEntityExists<ApiCall>(apiCallId.Value, Localizer["apiCall.notFound"], cancellationToken);
                    break;

                case PipelineStepType.JavaScriptFunction:
                    if (!javaScriptFunctionId.HasValue)
                    {
                        throw new InvalidOperationException("pipeline.step.javaScriptFunctionId.required");
                    }

                    await EnsureEntityExists<JavaScriptFunction>(javaScriptFunctionId.Value, Localizer["javaScriptFunction.notFound"], cancellationToken);
                    break;

                case PipelineStepType.ExecuteScript:
                    if (!databaseScriptId.HasValue)
                    {
                        throw new InvalidOperationException("pipeline.step.databaseScriptId.required");
                    }

                    await EnsureEntityExists<DatabaseScript>(databaseScriptId.Value, Localizer["database.script.notFound"], cancellationToken);
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

        public override async Task<PipelineStep?> Delete(long id, CancellationToken cancellationToken = default)
        {
            MutableMessages.Clear();

            PipelineStep? step = await DbContext.Set<PipelineStep>()
                .AsNoTracking()
                .FirstOrDefaultAsync(current => current.Id == id, cancellationToken);

            if (step is null)
            {
                MutableMessages.Add(new KeyNotFoundException(Localizer["pipeline.step.notFound"]));
                return null;
            }

            return await Delete([step], cancellationToken) ? step : null;
        }
    }
}
