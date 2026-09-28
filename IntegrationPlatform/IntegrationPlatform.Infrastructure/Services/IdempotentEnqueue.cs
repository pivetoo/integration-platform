using IntegrationPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace IntegrationPlatform.Infrastructure.Services
{
    // Insere um item na fila deduplicando por (conector, pipeline, chave de idempotencia). Chave repetida
    // devolve o item ja existente; a corrida entre duas requisicoes e resolvida pelo indice unico.
    internal static class IdempotentEnqueue
    {
        private const string PostgresUniqueViolation = "23505";
        private static readonly int[] SqlServerUniqueViolations = [2601, 2627];

        public static async Task<ProcessingQueue> AddOrGetExisting(DbContext dbContext, ProcessingQueue item, CancellationToken cancellationToken)
        {
            if (item.IdempotencyKey is null)
            {
                dbContext.Set<ProcessingQueue>().Add(item);
                await dbContext.SaveChangesAsync(cancellationToken);
                return item;
            }

            ProcessingQueue? existing = await Find(dbContext, item, cancellationToken);
            if (existing is not null)
            {
                return existing;
            }

            dbContext.Set<ProcessingQueue>().Add(item);

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                return item;
            }
            catch (DbUpdateException exception) when (IsUniqueViolation(exception))
            {
                dbContext.Entry(item).State = EntityState.Detached;

                return await Find(dbContext, item, cancellationToken) ?? throw exception;
            }
        }

        private static Task<ProcessingQueue?> Find(DbContext dbContext, ProcessingQueue item, CancellationToken cancellationToken)
        {
            return dbContext.Set<ProcessingQueue>()
                .AsNoTracking()
                .FirstOrDefaultAsync(current => current.ConnectorId == item.ConnectorId
                    && current.PipelineId == item.PipelineId
                    && current.IdempotencyKey == item.IdempotencyKey, cancellationToken);
        }

        private static bool IsUniqueViolation(DbUpdateException exception)
        {
            return exception.InnerException switch
            {
                PostgresException postgres => postgres.SqlState == PostgresUniqueViolation,
                Microsoft.Data.SqlClient.SqlException sql => SqlServerUniqueViolations.Contains(sql.Number),
                _ => false
            };
        }
    }
}
