using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using IntegrationPlataform.Application.Localization;
using IntegrationPlataform.Application.Requests.PipelineExamples;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace IntegrationPlataform.Infrastructure.Services
{
    public sealed class PipelineExampleService : CrudService<PipelineExample>, IPipelineExampleService
    {
        private readonly IStringLocalizer<IntegrationPlataformResource> Localizer;

        public PipelineExampleService(DbContext dbContext, IStringLocalizer<IntegrationPlataformResource> localizer) : base(dbContext)
        {
            Localizer = localizer;
        }

        public async Task<PagedResult<PipelineExample>> GetPipelineExamples(PagedRequest request, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .OrderBy(item => item.Name)
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<PipelineExample?> GetPipelineExampleById(long id, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<List<PipelineExample>> GetByPipeline(long pipelineId, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .Where(item => item.PipelineId == pipelineId)
                .OrderByDescending(item => item.IsDefault)
                .ThenBy(item => item.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<PipelineExample> CreatePipelineExample(CreatePipelineExampleRequest request, CancellationToken cancellationToken = default)
        {
            await EnsurePipelineExists(request.PipelineId, cancellationToken);
            await EnsureUniqueName(request.PipelineId, request.Name, null, cancellationToken);
            await ClearDefaultIfNeeded(request.PipelineId, request.IsDefault, cancellationToken: cancellationToken);

            PipelineExample entity = new(request.PipelineId, request.Name, request.InputPayloadExample, request.ExpectedOutputExample, request.Description, request.IsDefault);
            bool success = await Insert(cancellationToken, entity);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetPipelineExampleById(entity.Id, cancellationToken) ?? entity;
        }

        public async Task<PipelineExample> UpdatePipelineExample(long id, UpdatePipelineExampleRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException(Localizer["request.route.idMismatch"]);
            }

            PipelineExample? entity = await DbContext.Set<PipelineExample>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (entity is null)
            {
                throw new InvalidOperationException(Localizer["pipelineExample.notFound"]);
            }

            await EnsurePipelineExists(request.PipelineId, cancellationToken);
            await EnsureUniqueName(request.PipelineId, request.Name, id, cancellationToken);
            await ClearDefaultIfNeeded(request.PipelineId, request.IsDefault, id, cancellationToken);

            entity.Update(request.PipelineId, request.Name, request.InputPayloadExample, request.ExpectedOutputExample, request.Description, request.IsDefault);

            PipelineExample? result = await Update(entity, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetPipelineExampleById(result.Id, cancellationToken) ?? result;
        }

        public override async Task<PipelineExample?> Delete(long id, CancellationToken cancellationToken = default)
        {
            MutableMessages.Clear();

            PipelineExample? entity = await QueryWithDetails()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (entity is null)
            {
                MutableMessages.Add(new KeyNotFoundException(Localizer["pipelineExample.notFound"]));
                return null;
            }

            return await Delete([entity], cancellationToken) ? entity : null;
        }

        private IQueryable<PipelineExample> QueryWithDetails()
        {
            return DbContext.Set<PipelineExample>()
                .AsNoTracking()
                .Include(item => item.Pipeline);
        }

        private async Task EnsurePipelineExists(long pipelineId, CancellationToken cancellationToken)
        {
            bool exists = await DbContext.Set<Pipeline>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == pipelineId, cancellationToken);

            if (!exists)
            {
                throw new InvalidOperationException(Localizer["pipeline.notFound"]);
            }
        }

        private async Task EnsureUniqueName(long pipelineId, string name, long? currentId, CancellationToken cancellationToken)
        {
            bool exists = await DbContext.Set<PipelineExample>()
                .AsNoTracking()
                .AnyAsync(item => item.PipelineId == pipelineId && item.Name == name && (!currentId.HasValue || item.Id != currentId.Value), cancellationToken);

            if (exists)
            {
                throw new InvalidOperationException(Localizer["pipelineExample.name.alreadyExistsForPipeline"]);
            }
        }

        private async Task ClearDefaultIfNeeded(long pipelineId, bool isDefault, long? currentId = null, CancellationToken cancellationToken = default)
        {
            if (!isDefault)
            {
                return;
            }

            List<PipelineExample> defaults = await DbContext.Set<PipelineExample>()
                .AsTracking()
                .Where(item => item.PipelineId == pipelineId && item.IsDefault && (!currentId.HasValue || item.Id != currentId.Value))
                .ToListAsync(cancellationToken);

            foreach (PipelineExample item in defaults)
            {
                item.Update(item.PipelineId, item.Name, item.InputPayloadExample, item.ExpectedOutputExample, item.Description, false);
            }
        }
    }
}
