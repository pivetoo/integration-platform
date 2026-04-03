namespace IntegrationPlataform.Infrastructure.Services.ExecutionEngine
{
    internal interface IDatabaseExecutor
    {
        Task<object> ExecuteQueryAsync(string connectionString, string query, int timeoutSeconds = 60, CancellationToken cancellationToken = default);
    }
}
