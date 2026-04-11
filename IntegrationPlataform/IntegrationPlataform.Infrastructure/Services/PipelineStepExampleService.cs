using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using IntegrationPlataform.Application.Localization;
using IntegrationPlataform.Application.Requests.PipelineStepExamples;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace IntegrationPlataform.Infrastructure.Services
{
    public sealed class PipelineStepExampleService : CrudService<PipelineStepExample>, IPipelineStepExampleService
    {
        private readonly IStringLocalizer<IntegrationPlataformResource> Localizer;

        public PipelineStepExampleService(DbContext dbContext, IStringLocalizer<IntegrationPlataformResource> localizer) : base(dbContext)
        {
            Localizer = localizer;
        }

        public async Task<PagedResult<PipelineStepExample>> GetPipelineStepExamples(PagedRequest request, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .OrderBy(item => item.Name)
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<PipelineStepExample?> GetPipelineStepExampleById(long id, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<List<PipelineStepExample>> GetByPipelineStep(long pipelineStepId, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .Where(item => item.PipelineStepId == pipelineStepId)
                .OrderByDescending(item => item.IsDefault)
                .ThenBy(item => item.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<PipelineStepExample> CreatePipelineStepExample(CreatePipelineStepExampleRequest request, CancellationToken cancellationToken = default)
        {
            await EnsurePipelineStepExists(request.PipelineStepId, cancellationToken);
            await EnsurePipelineExampleExists(request.PipelineExampleId, cancellationToken);
            await ClearDefaultIfNeeded(request.PipelineStepId, request.IsDefault, cancellationToken: cancellationToken);

            PipelineStepExample entity = new(request.PipelineStepId, request.Name, request.RequestExample, request.ResponseExample, request.PipelineExampleId, request.IsDefault);
            bool success = await Insert(cancellationToken, entity);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetPipelineStepExampleById(entity.Id, cancellationToken) ?? entity;
        }

        public async Task<PipelineStepExample> UpdatePipelineStepExample(long id, UpdatePipelineStepExampleRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException(Localizer["request.route.idMismatch"]);
            }

            PipelineStepExample? entity = await DbContext.Set<PipelineStepExample>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (entity is null)
            {
                throw new InvalidOperationException(Localizer["pipelineStepExample.notFound"]);
            }

            await EnsurePipelineStepExists(request.PipelineStepId, cancellationToken);
            await EnsurePipelineExampleExists(request.PipelineExampleId, cancellationToken);
            await ClearDefaultIfNeeded(request.PipelineStepId, request.IsDefault, id, cancellationToken);

            entity.Update(request.PipelineStepId, request.Name, request.RequestExample, request.ResponseExample, request.PipelineExampleId, request.IsDefault);

            PipelineStepExample? result = await Update(entity, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetPipelineStepExampleById(result.Id, cancellationToken) ?? result;
        }

        public override async Task<PipelineStepExample?> Delete(long id, CancellationToken cancellationToken = default)
        {
            MutableMessages.Clear();

            PipelineStepExample? entity = await QueryWithDetails()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (entity is null)
            {
                MutableMessages.Add(new KeyNotFoundException(Localizer["pipelineStepExample.notFound"]));
                return null;
            }

            return await Delete([entity], cancellationToken) ? entity : null;
        }

        private IQueryable<PipelineStepExample> QueryWithDetails()
        {
            return DbContext.Set<PipelineStepExample>()
                .AsNoTracking()
                .Include(item => item.PipelineStep)
                .Include(item => item.PipelineExample);
        }

        private async Task EnsurePipelineStepExists(long pipelineStepId, CancellationToken cancellationToken)
        {
            bool exists = await DbContext.Set<PipelineStep>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == pipelineStepId, cancellationToken);

            if (!exists)
            {
                throw new InvalidOperationException(Localizer["pipelineStep.notFound"]);
            }
        }

        private async Task EnsurePipelineExampleExists(long? pipelineExampleId, CancellationToken cancellationToken)
        {
            if (!pipelineExampleId.HasValue)
            {
                return;
            }

            bool exists = await DbContext.Set<PipelineExample>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == pipelineExampleId.Value, cancellationToken);

            if (!exists)
            {
                throw new InvalidOperationException(Localizer["pipelineExample.notFound"]);
            }
        }

        private async Task ClearDefaultIfNeeded(long pipelineStepId, bool isDefault, long? currentId = null, CancellationToken cancellationToken = default)
        {
            if (!isDefault)
            {
                return;
            }

            List<PipelineStepExample> defaults = await DbContext.Set<PipelineStepExample>()
                .AsTracking()
                .Where(item => item.PipelineStepId == pipelineStepId && item.IsDefault && (!currentId.HasValue || item.Id != currentId.Value))
                .ToListAsync(cancellationToken);

            foreach (PipelineStepExample item in defaults)
            {
                item.Update(item.PipelineStepId, item.Name, item.RequestExample, item.ResponseExample, item.PipelineExampleId, false);
            }
        }
    }
}
