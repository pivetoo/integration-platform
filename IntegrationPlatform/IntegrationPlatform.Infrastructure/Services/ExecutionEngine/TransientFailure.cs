namespace IntegrationPlatform.Infrastructure.Services.ExecutionEngine
{
    // Classifica falha de step HTTP como transitoria (vale reexecutar) ou permanente. Sem status HTTP
    // (rede/timeout), 408, 429 e 5xx sao transitorias; demais 4xx nunca se resolvem sozinhos.
    public static class TransientFailure
    {
        public static bool IsTransientStatus(int? statusCode)
        {
            return statusCode is null or 408 or 429 or >= 500;
        }

        public static bool IsTransientException(Exception exception, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            return exception is HttpRequestException or TaskCanceledException;
        }
    }
}
