using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using IntegrationPlataform.Application.Localization;
using IntegrationPlataform.Application.Requests.Pipelines;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace IntegrationPlataform.Infrastructure.Services
{
    public sealed class PipelineService : CrudService<Pipeline>, IPipelineService
    {
        private readonly IStringLocalizer<IntegrationPlataformResource> Localizer;

        public PipelineService(DbContext dbContext, IStringLocalizer<IntegrationPlataformResource> localizer) : base(dbContext)
        {
            Localizer = localizer;
        }

        public async Task<PagedResult<Pipeline>> GetPipelines(PagedRequest request, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .OrderBy(item => item.Name)
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<Pipeline?> GetPipelineById(long id, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<List<Pipeline>> GetPipelinesByIntegration(long integrationId, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .Where(item => item.IntegrationId == integrationId)
                .OrderBy(item => item.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Pipeline>> GetActivePipelines(CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .Where(item => item.IsActive)
                .OrderBy(item => item.Name)
                .ToListAsync(cancellationToken);
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

            return await GetPipelineById(pipeline.Id, cancellationToken) ?? pipeline;
        }

        public async Task<Pipeline> UpdatePipeline(long id, UpdatePipelineRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException(Localizer["request.route.idMismatch"]);
            }

            Pipeline? pipeline = await DbContext.Set<Pipeline>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (pipeline is null)
            {
                throw new InvalidOperationException(Localizer["pipeline.notFound"]);
            }

            await EnsureIntegrationExists(request.IntegrationId, cancellationToken);
            await EnsureUniqueIdentifier(request.IntegrationId, request.Identifier, id, cancellationToken);

            pipeline.Update(request.IntegrationId, request.Identifier, request.Name, request.Description, request.IsActive);

            Pipeline? result = await Update(pipeline, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetPipelineById(result.Id, cancellationToken) ?? result;
        }

        private async Task EnsureIntegrationExists(long integrationId, CancellationToken cancellationToken)
        {
            bool exists = await DbContext.Set<Integration>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == integrationId, cancellationToken);

            if (!exists)
            {
                throw new InvalidOperationException(Localizer["integration.notFound"]);
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
                throw new InvalidOperationException(Localizer["pipeline.identifier.alreadyExistsForIntegration"]);
            }
        }

        public override async Task<Pipeline?> Delete(long id, CancellationToken cancellationToken = default)
        {
            MutableMessages.Clear();

            Pipeline? pipeline = await QueryWithDetails()
                .FirstOrDefaultAsync(current => current.Id == id, cancellationToken);

            if (pipeline is null)
            {
                MutableMessages.Add(new KeyNotFoundException(Localizer["pipeline.notFound"]));
                return null;
            }

            return await Delete([pipeline], cancellationToken) ? pipeline : null;
        }

        private IQueryable<Pipeline> QueryWithDetails()
        {
            return DbContext.Set<Pipeline>()
                .AsNoTracking()
                .Include(item => item.Integration)
                .ThenInclude(item => item!.IntegrationCategory);
        }
    }
}
