using Archon.Infrastructure.Services;
using IntegrationPlataform.Application.Requests.Pipelines;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlataform.Infrastructure.Services
{
    public sealed class PipelineService : CrudService<Pipeline>, IPipelineService
    {
        public PipelineService(DbContext dbContext) : base(dbContext)
        {
        }

        public async Task<Pipeline> CreatePipeline(CreatePipelineRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureIntegrationExists(request.IntegrationId, cancellationToken);
            await EnsureUniqueIdentifier(request.IntegrationId, request.Identifier, null, cancellationToken);

            Pipeline pipeline = new(request.IntegrationId, request.Identifier, request.Name, request.Description);
            bool success = await Insert(cancellationToken, pipeline);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return pipeline;
        }

        public async Task<Pipeline> UpdatePipeline(long id, UpdatePipelineRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException("Route id does not match body id.");
            }

            Pipeline? pipeline = await DbContext.Set<Pipeline>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (pipeline is null)
            {
                throw new InvalidOperationException("Pipeline not found.");
            }

            await EnsureIntegrationExists(request.IntegrationId, cancellationToken);
            await EnsureUniqueIdentifier(request.IntegrationId, request.Identifier, id, cancellationToken);

            pipeline.Update(request.IntegrationId, request.Identifier, request.Name, request.Description, request.IsActive);

            Pipeline? result = await Update(pipeline, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return result;
        }

        private async Task EnsureIntegrationExists(long integrationId, CancellationToken cancellationToken)
        {
            bool exists = await DbContext.Set<Integration>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == integrationId, cancellationToken);

            if (!exists)
            {
                throw new InvalidOperationException("Integration not found.");
            }
        }

        private async Task EnsureUniqueIdentifier(long integrationId, string identifier, long? currentId, CancellationToken cancellationToken)
        {
            bool exists = await DbContext.Set<Pipeline>()
                .AsNoTracking()
                .AnyAsync(
                    item => item.IntegrationId == integrationId &&
                    item.Identifier == identifier &&
                    (!currentId.HasValue || item.Id != currentId.Value),
                    cancellationToken);

            if (exists)
            {
                throw new InvalidOperationException("Pipeline identifier already exists for this integration.");
            }
        }
    }
}
