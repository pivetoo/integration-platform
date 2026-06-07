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

        public async Task<PagedResult<PipelineRoutine>> GetPipelineRoutines(PagedRequest request, string? search, CancellationToken cancellationToken = default)
        {
            var query = DbContext.Set<PipelineRoutine>()
                .AsNoTracking()
                .Include(item => item.Connector)
                    .ThenInclude(c => c!.Integration)
                    .ThenInclude(i => i!.IntegrationCategory)
                .Include(item => item.Pipeline)
                    .ThenInclude(p => p!.Integration)
                    .ThenInclude(i => i!.IntegrationCategory);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lower = search.ToLower();
                return await query
                    .Where(item =>
                        (item.Connector != null && item.Connector.Name.ToLower().Contains(lower)) ||
                        (item.Pipeline != null && item.Pipeline.Name.ToLower().Contains(lower)))
                    .OrderByDescending(item => item.CreatedAt)
                    .ToPagedResultAsync(request, cancellationToken);
            }

            return await query
                .OrderByDescending(item => item.CreatedAt)
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<PipelineRoutine?> GetPipelineRoutineById(long id, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<PipelineRoutine>()
                .AsNoTracking()
                .Include(item => item.Connector)
                    .ThenInclude(c => c!.Integration)
                    .ThenInclude(i => i!.IntegrationCategory)
                .Include(item => item.Pipeline)
                    .ThenInclude(p => p!.Integration)
                    .ThenInclude(i => i!.IntegrationCategory)
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
                throw new InvalidOperationException("pipeline.routine.notFound");
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
