using IntegrationPlatform.Domain.Entities;

namespace IntegrationPlatform.Infrastructure.Services.ExecutionEngine
{
    // Chave de idempotencia exposta ao pipeline como {{idempotencyKey}}. O interpolador le as variaveis de
    // step antes do payload, entao se o payload ja traz a propria chave (seeds de pagamento ecoam esse
    // valor nos callbacks) nao se cria a variavel, para nao sombrear o valor do consumidor.
    public static class IdempotencyKeyResolver
    {
        public const string VariableName = "idempotencyKey";

        public static string? Resolve(ProcessingQueue queueItem, Dictionary<string, object> payloadData)
        {
            if (payloadData.Keys.Any(key => string.Equals(key, VariableName, StringComparison.OrdinalIgnoreCase)))
            {
                return null;
            }

            return queueItem.IdempotencyKey ?? $"q{queueItem.Id}";
        }
    }
}
