using Archon.Infrastructure.Services;
using IntegrationPlataform.Application.Localization;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace IntegrationPlataform.Infrastructure.Services
{
    public sealed class IntegrationAttributeService : CrudService<IntegrationAttribute>, IIntegrationAttributeService
    {
        private readonly IStringLocalizer<IntegrationPlataformResource> Localizer;

        public IntegrationAttributeService(DbContext dbContext, IStringLocalizer<IntegrationPlataformResource> localizer) : base(dbContext)
        {
            Localizer = localizer;
        }

        public override async Task<IntegrationAttribute?> Delete(long id, CancellationToken cancellationToken = default)
        {
            MutableMessages.Clear();

            IntegrationAttribute? attribute = await DbContext.Set<IntegrationAttribute>()
                .AsNoTracking()
                .FirstOrDefaultAsync(current => current.Id == id, cancellationToken);

            if (attribute is null)
            {
                MutableMessages.Add(new KeyNotFoundException(Localizer["integration.attribute.notFound"]));
                return null;
            }

            return await Delete([attribute], cancellationToken) ? attribute : null;
        }
    }
}
