using Archon.Infrastructure.Services;
using IntegrationPlataform.Application.Localization;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace IntegrationPlataform.Infrastructure.Services
{
    public sealed class ExecutionLogService : CrudService<ExecutionLog>, IExecutionLogService
    {
        private readonly IStringLocalizer<IntegrationPlataformResource> Localizer;

        public ExecutionLogService(DbContext dbContext, IStringLocalizer<IntegrationPlataformResource> localizer) : base(dbContext)
        {
            Localizer = localizer;
        }

        public override async Task<ExecutionLog?> Delete(long id, CancellationToken cancellationToken = default)
        {
            MutableMessages.Clear();

            ExecutionLog? log = await DbContext.Set<ExecutionLog>()
                .AsNoTracking()
                .FirstOrDefaultAsync(current => current.Id == id, cancellationToken);

            if (log is null)
            {
                MutableMessages.Add(new KeyNotFoundException(Localizer["execution.log.notFound"]));
                return null;
            }

            return await Delete([log], cancellationToken) ? log : null;
        }
    }
}
