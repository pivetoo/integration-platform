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

        public override async Task<ProcessingQueue?> Delete(long id, CancellationToken cancellationToken = default)
        {
            MutableMessages.Clear();

            ProcessingQueue? queue = await DbContext.Set<ProcessingQueue>()
                .AsNoTracking()
                .FirstOrDefaultAsync(current => current.Id == id, cancellationToken);

            if (queue is null)
            {
                MutableMessages.Add(new KeyNotFoundException(Localizer["processingQueue.notFound"]));
                return null;
            }

            return await Delete([queue], cancellationToken) ? queue : null;
        }
    }
}
