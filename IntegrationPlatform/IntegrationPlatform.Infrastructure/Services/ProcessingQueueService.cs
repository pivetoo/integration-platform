using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using IntegrationPlatform.Application.Localization;
using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace IntegrationPlatform.Infrastructure.Services
{
    public sealed class ProcessingQueueService : CrudService<ProcessingQueue>, IProcessingQueueService
    {
        private readonly IStringLocalizer<IntegrationPlatformResource> Localizer;

        public ProcessingQueueService(DbContext dbContext, IStringLocalizer<IntegrationPlatformResource> localizer) : base(dbContext)
        {
            Localizer = localizer;
        }

        public async Task<PagedResult<ProcessingQueue>> GetProcessingQueues(PagedRequest request, string? search, CancellationToken cancellationToken = default)
        {
            var query = QueryWithDetails();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var lower = search.ToLower();
                query = query.Where(item =>
                    (item.Connector != null && item.Connector.Name.ToLower().Contains(lower)) ||
                    (item.Pipeline != null && item.Pipeline.Name.ToLower().Contains(lower)));
            }
            return await query
                .OrderByDescending(item => item.CreatedAt)
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<ProcessingQueue?> GetProcessingQueueById(long id, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<List<ProcessingQueue>> GetPendingProcessingQueues(CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .Where(item => item.Status == Domain.ValueObjects.ProcessingStatus.Pending &&
                    (!item.ScheduledAt.HasValue || item.ScheduledAt <= DateTimeOffset.UtcNow))
                .OrderBy(item => item.Priority)
                .ThenBy(item => item.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public override async Task<ProcessingQueue?> Delete(long id, CancellationToken cancellationToken = default)
        {
            MutableMessages.Clear();

            ProcessingQueue? queue = await GetProcessingQueueById(id, cancellationToken);

            if (queue is null)
            {
                MutableMessages.Add(new KeyNotFoundException(Localizer["processingQueue.notFound"]));
                return null;
            }

            return await Delete([queue], cancellationToken) ? queue : null;
        }

        private IQueryable<ProcessingQueue> QueryWithDetails()
        {
            return DbContext.Set<ProcessingQueue>()
                .AsNoTracking()
                .Include(item => item.Connector)
                .ThenInclude(item => item!.Integration)
                .ThenInclude(item => item!.IntegrationCategory)
                .Include(item => item.Pipeline)
                .ThenInclude(item => item!.Integration)
                .ThenInclude(item => item!.IntegrationCategory);
        }
    }
}
