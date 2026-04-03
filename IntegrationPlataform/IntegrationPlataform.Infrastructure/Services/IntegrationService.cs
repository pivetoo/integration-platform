using Archon.Infrastructure.Services;
using IntegrationPlataform.Application.Requests.Integrations;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Infrastructure.Services
{
    public sealed class IntegrationService : CrudService<Integration>, IIntegrationService
    {
        public IntegrationService(DbContext dbContext) : base(dbContext)
        {
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
                throw new InvalidOperationException("Route id does not match body id.");
            }

            Integration? integration = await DbContext.Set<Integration>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (integration is null)
            {
                throw new InvalidOperationException("Integration not found.");
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
                throw new InvalidOperationException("Integration identifier already exists.");
            }
        }
    }
}
