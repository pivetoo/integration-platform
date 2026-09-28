namespace IntegrationPlatform.Infrastructure.Services.ExecutionEngine
{
    // Politica de reentrega (fila e callbacks): dado o numero de tentativas ja feitas, decide o atraso ate a
    // proxima tentativa (backoff exponencial baseBackoffSeconds * 2^attempts) ou null quando atingiu o
    // maximo de tentativas (falha permanente). Logica pura, isolada para ser testavel.
    public static class RetryPolicy
    {
        public static TimeSpan? NextRetryDelay(int attempts, int maxAttempts, double baseBackoffSeconds)
        {
            if (attempts + 1 >= maxAttempts)
            {
                return null;
            }

            double seconds = baseBackoffSeconds * Math.Pow(2, attempts);
            return TimeSpan.FromSeconds(seconds);
        }
    }
}
