using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using IntegrationPlataform.Application.Localization;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace IntegrationPlataform.Infrastructure.Services
{
    public sealed class ProcessingQueueService : CrudService<ProcessingQueue>, IProcessingQueueService
    {
        private readonly IStringLocalizer<IntegrationPlataformResource> Localizer;

        public ProcessingQueueService(DbContext dbContext, IStringLocalizer<IntegrationPlataformResource> localizer) : base(dbContext)
        {
            Localizer = localizer;
        }

        public async Task<PagedResult<ProcessingQueue>> GetProcessingQueues(PagedRequest request, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
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
