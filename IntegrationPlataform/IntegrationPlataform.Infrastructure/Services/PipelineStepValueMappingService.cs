using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using IntegrationPlataform.Application.Localization;
using IntegrationPlataform.Application.Requests.PipelineStepValueMappings;
using IntegrationPlataform.Application.Services;
using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace IntegrationPlataform.Infrastructure.Services
{
    public sealed class PipelineStepValueMappingService : CrudService<PipelineStepValueMapping>, IPipelineStepValueMappingService
    {
        private readonly IStringLocalizer<IntegrationPlataformResource> Localizer;

        public PipelineStepValueMappingService(DbContext dbContext, IStringLocalizer<IntegrationPlataformResource> localizer) : base(dbContext)
        {
            Localizer = localizer;
        }

        public async Task<PagedResult<PipelineStepValueMapping>> GetPipelineStepValueMappings(PagedRequest request, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .OrderBy(item => item.Order)
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<PipelineStepValueMapping?> GetPipelineStepValueMappingById(long id, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<List<PipelineStepValueMapping>> GetByPipelineStep(long pipelineStepId, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .Where(item => item.PipelineStepId == pipelineStepId)
                .OrderBy(item => item.Order)
                .ToListAsync(cancellationToken);
        }

        public async Task<PipelineStepValueMapping> CreatePipelineStepValueMapping(CreatePipelineStepValueMappingRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureReferences(request.PipelineStepId, request.SourcePipelineStepId, request.PipelineExampleId, cancellationToken);

            PipelineStepValueMapping entity = new(
                request.PipelineStepId,
                request.TargetField,
                request.SourceType,
                request.SourcePath,
                request.ValueType,
                request.Order,
                request.FixedValue,
                request.SourcePipelineStepId,
                request.PipelineExampleId);

            bool success = await Insert(cancellationToken, entity);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetPipelineStepValueMappingById(entity.Id, cancellationToken) ?? entity;
        }

        public async Task<PipelineStepValueMapping> UpdatePipelineStepValueMapping(long id, UpdatePipelineStepValueMappingRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException(Localizer["request.route.idMismatch"]);
            }

            PipelineStepValueMapping? entity = await DbContext.Set<PipelineStepValueMapping>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (entity is null)
            {
                throw new InvalidOperationException(Localizer["pipelineStepValueMapping.notFound"]);
            }

            await EnsureReferences(request.PipelineStepId, request.SourcePipelineStepId, request.PipelineExampleId, cancellationToken);

            entity.Update(
                request.PipelineStepId,
                request.TargetField,
                request.SourceType,
                request.SourcePath,
                request.ValueType,
                request.Order,
                request.FixedValue,
                request.SourcePipelineStepId,
                request.PipelineExampleId);

            PipelineStepValueMapping? result = await Update(entity, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetPipelineStepValueMappingById(result.Id, cancellationToken) ?? result;
        }

        public override async Task<PipelineStepValueMapping?> Delete(long id, CancellationToken cancellationToken = default)
        {
            MutableMessages.Clear();

            PipelineStepValueMapping? entity = await QueryWithDetails()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (entity is null)
            {
                MutableMessages.Add(new KeyNotFoundException(Localizer["pipelineStepValueMapping.notFound"]));
                return null;
            }

            return await Delete([entity], cancellationToken) ? entity : null;
        }

        private IQueryable<PipelineStepValueMapping> QueryWithDetails()
        {
            return DbContext.Set<PipelineStepValueMapping>()
                .AsNoTracking()
                .Include(item => item.PipelineStep)
                .Include(item => item.SourcePipelineStep)
                .Include(item => item.PipelineExample);
        }

        private async Task EnsureReferences(long pipelineStepId, long? sourcePipelineStepId, long? pipelineExampleId, CancellationToken cancellationToken)
        {
            bool pipelineStepExists = await DbContext.Set<PipelineStep>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == pipelineStepId, cancellationToken);

            if (!pipelineStepExists)
            {
                throw new InvalidOperationException(Localizer["pipelineStep.notFound"]);
            }

            if (sourcePipelineStepId.HasValue)
            {
                bool sourceExists = await DbContext.Set<PipelineStep>()
                    .AsNoTracking()
                    .AnyAsync(item => item.Id == sourcePipelineStepId.Value, cancellationToken);

                if (!sourceExists)
                {
                    throw new InvalidOperationException(Localizer["pipelineStep.notFound"]);
                }
            }

            if (pipelineExampleId.HasValue)
            {
                bool pipelineExampleExists = await DbContext.Set<PipelineExample>()
                    .AsNoTracking()
                    .AnyAsync(item => item.Id == pipelineExampleId.Value, cancellationToken);

                if (!pipelineExampleExists)
                {
                    throw new InvalidOperationException(Localizer["pipelineExample.notFound"]);
                }
            }
        }
    }
}
