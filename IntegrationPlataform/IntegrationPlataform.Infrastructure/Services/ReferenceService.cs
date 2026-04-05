using Archon.Infrastructure.Services;
using IntegrationPlataform.Application.Localization;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace IntegrationPlataform.Infrastructure.Services
{
    public sealed class ReferenceService : CrudService<Reference>, IReferenceService
    {
        private readonly IStringLocalizer<IntegrationPlataformResource> Localizer;

        public ReferenceService(DbContext dbContext, IStringLocalizer<IntegrationPlataformResource> localizer) : base(dbContext)
        {
            Localizer = localizer;
        }

        public override async Task<Reference?> Delete(long id, CancellationToken cancellationToken = default)
        {
            MutableMessages.Clear();

            Reference? reference = await DbContext.Set<Reference>()
                .AsNoTracking()
                .FirstOrDefaultAsync(current => current.Id == id, cancellationToken);

            if (reference is null)
            {
                MutableMessages.Add(new KeyNotFoundException(Localizer["reference.notFound"]));
                return null;
            }

            return await Delete([reference], cancellationToken) ? reference : null;
        }
    }
}
