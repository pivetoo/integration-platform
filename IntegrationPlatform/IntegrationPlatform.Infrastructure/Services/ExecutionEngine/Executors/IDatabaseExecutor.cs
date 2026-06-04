namespace IntegrationPlatform.Infrastructure.Services.ExecutionEngine
{
    internal interface IDatabaseExecutor
    {
        Task<object> ExecuteQueryAsync(string connectionString, string query, IReadOnlyList<SqlScriptParameter> parameters, int timeoutSeconds = 60, CancellationToken cancellationToken = default);
    }
}
