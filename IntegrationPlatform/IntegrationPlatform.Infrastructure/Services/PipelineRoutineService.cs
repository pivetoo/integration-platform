using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using IntegrationPlatform.Application.Localization;
using IntegrationPlatform.Application.Requests.PipelineRoutines;
using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace IntegrationPlatform.Infrastructure.Services
{
    public sealed class PipelineRoutineService : CrudService<PipelineRoutine>, IPipelineRoutineService
    {
        private readonly IStringLocalizer<IntegrationPlatformResource> Localizer;

        public async Task<PagedResult<PipelineRoutine>> GetPipelineRoutines(PagedRequest request, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<PipelineRoutine>()
                .AsNoTracking()
                .OrderByDescending(item => item.CreatedAt)
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<PipelineRoutine?> GetPipelineRoutineById(long id, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<PipelineRoutine>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<PipelineRoutine> CreatePipelineRoutine(CreatePipelineRoutineRequest request, CancellationToken cancellationToken = default)
        {
            PipelineRoutine routine = new(
                request.ConnectorId,
                request.PipelineId,
                request.IntervalMinutes,
                request.IsActive,
                request.DefaultPayload);

            routine.Update(request.IntervalMinutes, request.IsActive, request.DefaultPayload, request.NextExecution);

            bool success = await Insert(cancellationToken, routine);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetPipelineRoutineById(routine.Id, cancellationToken) ?? routine;
        }

        public async Task<PipelineRoutine> UpdatePipelineRoutine(
            long id,
            int intervalMinutes,
            bool isActive,
            string? defaultPayload,
            DateTimeOffset? nextExecution,
            CancellationToken cancellationToken = default)
        {
            PipelineRoutine? routine = await DbContext.Set<PipelineRoutine>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (routine is null)
            {
                throw new InvalidOperationException(Localizer["pipeline.routine.notFound"]);
            }

            routine.Update(intervalMinutes, isActive, defaultPayload, nextExecution);

            PipelineRoutine? result = await Update(routine, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetPipelineRoutineById(result.Id, cancellationToken) ?? result;
        }

        public PipelineRoutineService(DbContext dbContext, IStringLocalizer<IntegrationPlatformResource> localizer) : base(dbContext)
        {
            Localizer = localizer;
        }

        public override async Task<PipelineRoutine?> Delete(long id, CancellationToken cancellationToken = default)
        {
            MutableMessages.Clear();

            PipelineRoutine? routine = await GetPipelineRoutineById(id, cancellationToken);
            if (routine is null)
            {
                MutableMessages.Add(new KeyNotFoundException(Localizer["pipeline.routine.notFound"]));
                return null;
            }

            return await Delete([routine], cancellationToken) ? routine : null;
        }
    }
}
