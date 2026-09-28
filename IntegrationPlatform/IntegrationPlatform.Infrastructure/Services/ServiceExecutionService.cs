using Archon.Infrastructure.Persistence.EF;
using IntegrationPlatform.Application.Localization;
using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using IntegrationPlatform.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace IntegrationPlatform.Infrastructure.Services
{
    public sealed class ServiceExecutionService : IServiceExecutionService
    {
        private readonly DbContext dbContext;
        private readonly IExecutionEngineService executionEngineService;
        private readonly IStringLocalizer<IntegrationPlatformResource> Localizer;

        public ServiceExecutionService(DbContext dbContext, IExecutionEngineService executionEngineService, IStringLocalizer<IntegrationPlatformResource> localizer)
        {
            this.dbContext = dbContext;
            this.executionEngineService = executionEngineService;
            Localizer = localizer;
        }

        public async Task<Execution> ExecuteService(string serviceIdentifier, long connectorId, string? inputData, CancellationToken cancellationToken = default)
        {
            ResolvedTarget target = await ResolveTarget(serviceIdentifier, connectorId, cancellationToken);

            return await executionEngineService.ExecutePipeline(
                target.ConnectorId,
                target.PipelineId,
                inputData,
                ExecutionType.Manual,
                cancellationToken: cancellationToken);
        }

        public async Task<ProcessingQueue> EnqueueService(string serviceIdentifier, long connectorId, string? inputData, int priority = 5, DateTime? scheduledFor = null, string? idempotencyKey = null, CancellationToken cancellationToken = default)
        {
            ResolvedTarget target = await ResolveTarget(serviceIdentifier, connectorId, cancellationToken);

            DateTimeOffset? scheduledAt = scheduledFor.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(scheduledFor.Value, DateTimeKind.Utc)) : null;

            ProcessingQueue queue = new(target.ConnectorId, target.PipelineId, priority, ProcessingStatus.Pending, inputData, scheduledAt, idempotencyKey);
            return await IdempotentEnqueue.AddOrGetExisting(dbContext, queue, cancellationToken);
        }

        private async Task<ResolvedTarget> ResolveTarget(string serviceIdentifier, long connectorId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(serviceIdentifier))
            {
                throw new InvalidOperationException("serviceContract.identifier.required");
            }

            if (connectorId <= 0)
            {
                throw new InvalidOperationException("request.connector.id.required");
            }

            string normalizedIdentifier = serviceIdentifier.Trim().ToLowerInvariant();

            ServiceContract? service = await dbContext.Set<ServiceContract>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Identifier == normalizedIdentifier && item.IsActive, cancellationToken);

            if (service is null)
            {
                throw new InvalidOperationException("serviceContract.notFound");
            }

            Connector? connector = await dbContext.Set<Connector>()
                .AsNoTracking()
                .Include(item => item.Integration)
                .FirstOrDefaultAsync(item => item.Id == connectorId, cancellationToken);

            if (connector is null)
            {
                throw new InvalidOperationException("connector.notFound");
            }

            // Desativar conector ou integracao e kill switch, nao filtro de catalogo: sem esta guarda a
            // integracao inativa some das listagens mas o consumidor continua disparando chamada real ao
            // provedor pelo contrato de servico.
            if (!connector.IsActive)
            {
                throw new InvalidOperationException("connector.notActive");
            }

            if (connector.Integration is null || !connector.Integration.IsActive)
            {
                throw new InvalidOperationException("integration.notActive");
            }

            bool integrationSupportsService = await dbContext.Set<IntegrationServiceContract>()
                .AsNoTracking()
                .AnyAsync(item =>
                    item.IntegrationId == connector.IntegrationId
                    && item.ServiceContractId == service.Id
                    && item.IsActive,
                    cancellationToken);

            if (!integrationSupportsService)
            {
                throw new InvalidOperationException("serviceContract.integration.notSupported");
            }

            Pipeline? pipeline = await dbContext.Set<Pipeline>()
                .AsNoTracking()
                .Where(item =>
                    item.IntegrationId == connector.IntegrationId
                    && item.ServiceContractId == service.Id
                    && item.IsActive)
                .OrderByDescending(item => item.IsDefault)
                .ThenBy(item => item.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (pipeline is null)
            {
                throw new InvalidOperationException("serviceContract.pipeline.notFound");
            }

            return new ResolvedTarget(connector.Id, pipeline.Id);
        }

        private readonly record struct ResolvedTarget(long ConnectorId, long PipelineId);
    }
}
