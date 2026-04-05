using Archon.Infrastructure.Services;
using IntegrationPlataform.Application.Localization;
using IntegrationPlataform.Application.Requests.Integrations;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace IntegrationPlataform.Infrastructure.Services
{
    public sealed class IntegrationService : CrudService<Integration>, IIntegrationService
    {
        private readonly IStringLocalizer<IntegrationPlataformResource> Localizer;

        public IntegrationService(DbContext dbContext, IStringLocalizer<IntegrationPlataformResource> localizer) : base(dbContext)
        {
            Localizer = localizer;
        }

        public async Task<Integration> CreateIntegration(CreateIntegrationRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureUniqueIdentifier(request.Identifier, null, cancellationToken);

            Integration integration = new(request.Identifier, request.Name, request.Description, request.IntegrationCategoryId);
            bool success = await Insert(cancellationToken, integration);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return integration;
        }

        public async Task<Integration> UpdateIntegration(long id, UpdateIntegrationRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException(Localizer["request.route.idMismatch"]);
            }

            Integration? integration = await DbContext.Set<Integration>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (integration is null)
            {
                throw new InvalidOperationException(Localizer["integration.notFound"]);
            }

            await EnsureUniqueIdentifier(request.Identifier, id, cancellationToken);

            integration.Update(request.Identifier, request.Name, request.Description, request.IntegrationCategoryId, request.IsActive);

            Integration? result = await Update(integration, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return result;
        }

        private async Task EnsureUniqueIdentifier(string identifier, long? currentId, CancellationToken cancellationToken)
        {
            bool identifierExists = await DbContext.Set<Integration>()
                .AsNoTracking()
                .AnyAsync(
                    item => item.Identifier == identifier &&
                    (!currentId.HasValue || item.Id != currentId.Value),
                    cancellationToken);

            if (identifierExists)
            {
                throw new InvalidOperationException(Localizer["integration.identifier.alreadyExists"]);
            }
        }

        public override async Task<Integration?> Delete(long id, CancellationToken cancellationToken = default)
        {
            MutableMessages.Clear();

            Integration? integration = await DbContext.Set<Integration>()
                .AsNoTracking()
                .FirstOrDefaultAsync(current => current.Id == id, cancellationToken);

            if (integration is null)
            {
                MutableMessages.Add(new KeyNotFoundException(Localizer["integration.notFound"]));
                return null;
            }

            return await Delete([integration], cancellationToken) ? integration : null;
        }
    }
}
