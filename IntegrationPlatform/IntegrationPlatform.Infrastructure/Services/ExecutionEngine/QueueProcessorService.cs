using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Domain.Entities;
using IntegrationPlatform.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace IntegrationPlatform.Infrastructure.Services.ExecutionEngine
{
    public sealed class QueueProcessorService : IQueueProcessorService
    {
        private readonly DbContext dbContext;
        private readonly IExecutionEngineService executionEngineService;

        public QueueProcessorService(DbContext dbContext, IExecutionEngineService executionEngineService)
        {
            this.dbContext = dbContext;
            this.executionEngineService = executionEngineService;
        }

        public async Task<IReadOnlyCollection<ProcessingQueue>> GetPendingToProcess(int maxItems, CancellationToken cancellationToken = default)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            List<ProcessingQueue> items = await (
                from item in dbContext.Set<ProcessingQueue>().AsNoTracking()
                where item.Status == ProcessingStatus.Pending &&
                      (!item.ScheduledAt.HasValue || item.ScheduledAt <= now)
                orderby item.Priority, item.CreatedAt
                select item)
                .Take(maxItems)
                .ToListAsync(cancellationToken);

            return items;
        }

        public async Task<int> RecoverStuckItems(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset cutoff = now - timeout;

            return await dbContext.Set<ProcessingQueue>()
                .Where(item => item.Status == ProcessingStatus.Processing
                    && item.StartedAt != null
                    && item.StartedAt < cutoff)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(item => item.Status, ProcessingStatus.Error)
                    .SetProperty(item => item.LastError, "Item interrompido: tempo de processamento excedido.")
                    .SetProperty(item => item.FinishedAt, (DateTimeOffset?)now)
                    .SetProperty(item => item.UpdatedAt, (DateTimeOffset?)now), cancellationToken);
        }

        // Fecha execucoes orfas presas em Running alem do timeout (processo morto a meio, falha de
        // persistencia pos-insert, ou cancelamento). Sem isso a Execution ficaria Running para sempre,
        // mesmo com o item da fila ja recuperado. Espelha RecoverStuckItems.
        public async Task<int> RecoverStuckExecutions(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset cutoff = now - timeout;

            return await dbContext.Set<Execution>()
                .Where(item => item.Status == ExecutionStatus.Running && item.StartedAt < cutoff)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(item => item.Status, ExecutionStatus.Error)
                    .SetProperty(item => item.Errors, "Execucao interrompida: presa em Running alem do timeout.")
                    .SetProperty(item => item.FinishedAt, (DateTimeOffset?)now)
                    .SetProperty(item => item.UpdatedAt, (DateTimeOffset?)now), cancellationToken);
        }

        public async Task ProcessItem(long processingQueueId, CancellationToken cancellationToken = default)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            // Claim atomico: so um worker consegue mover o item de Pending -> Processing.
            // Em deploy escalado (multiplas instancias) isso impede que dois workers peguem o mesmo item.
            int claimed = await dbContext.Set<ProcessingQueue>()
                .Where(item => item.Id == processingQueueId && item.Status == ProcessingStatus.Pending)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(item => item.Status, ProcessingStatus.Processing)
                    .SetProperty(item => item.StartedAt, (DateTimeOffset?)now)
                    .SetProperty(item => item.FinishedAt, (DateTimeOffset?)null)
                    .SetProperty(item => item.LastError, (string?)null)
                    .SetProperty(item => item.UpdatedAt, (DateTimeOffset?)now), cancellationToken);

            if (claimed == 0)
            {
                // Ja foi reivindicado por outro worker (ou nao esta mais Pending) -> nada a fazer.
                return;
            }

            ProcessingQueue item = await dbContext.Set<ProcessingQueue>()
                .AsTracking()
                .FirstAsync(current => current.Id == processingQueueId, cancellationToken);

            // Kill switch tardio: o item pode ter sido enfileirado antes de o conector/integracao ser
            // desativado. Antes de executar, reconfere; se estiver inativo, falha o item em vez de disparar
            // chamada real ao provedor. A execucao manual/debug do proprio IP nao passa por aqui, entao
            // continua sendo possivel testar uma integracao desligada.
            Connector? connector = await dbContext.Set<Connector>()
                .AsNoTracking()
                .Include(current => current.Integration)
                .FirstOrDefaultAsync(current => current.Id == item.ConnectorId, cancellationToken);

            if (connector is null || !connector.IsActive || connector.Integration is null || !connector.Integration.IsActive)
            {
                item.Fail("Conector ou integracao inativa: execucao bloqueada.", DateTimeOffset.UtcNow);
                item.SetUpdatedAt(DateTimeOffset.UtcNow);
                await dbContext.SaveChangesAsync(cancellationToken);
                return;
            }

            try
            {
                await executionEngineService.ExecutePipeline(
                    item.ConnectorId,
                    item.PipelineId,
                    item.Payload,
                    ExecutionType.Pipeline,
                    item,
                    null,
                    cancellationToken);
            }
            catch (Exception exception)
            {
                item.Fail(exception.Message, DateTimeOffset.UtcNow);
                item.SetUpdatedAt(DateTimeOffset.UtcNow);
                await dbContext.SaveChangesAsync(cancellationToken);
                throw;
            }
        }
    }
}
