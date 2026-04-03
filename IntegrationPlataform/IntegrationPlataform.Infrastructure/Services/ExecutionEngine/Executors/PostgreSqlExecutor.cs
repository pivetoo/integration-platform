using Npgsql;
using System.Text.RegularExpressions;

namespace IntegrationPlataform.Infrastructure.Services.ExecutionEngine
{
    internal sealed class PostgreSqlExecutor : IDatabaseExecutor
    {
        public async Task<object> ExecuteQueryAsync(string connectionString, string query, int timeoutSeconds = 60, CancellationToken cancellationToken = default)
        {
            ValidateQuery(query);

            await using NpgsqlConnection connection = new(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using NpgsqlCommand command = new(query, connection)
            {
                CommandTimeout = timeoutSeconds
            };

            string trimmedQuery = query.Trim().ToUpperInvariant();
            bool isSelect = trimmedQuery.StartsWith("SELECT", StringComparison.Ordinal) ||
                            trimmedQuery.StartsWith("WITH", StringComparison.Ordinal);

            if (isSelect)
            {
                List<Dictionary<string, object?>> results = [];

                await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    Dictionary<string, object?> row = [];
                    for (int index = 0; index < reader.FieldCount; index++)
                    {
                        row[reader.GetName(index)] = await reader.IsDBNullAsync(index, cancellationToken)
                            ? null
                            : reader.GetValue(index);
                    }

                    results.Add(row);
                }

                return results.Count == 1 ? results[0] : results;
            }

            int rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
            return new Dictionary<string, object?> { ["rowsAffected"] = rowsAffected };
        }

        private static void ValidateQuery(string query)
        {
            string normalized = Regex.Replace(query.Trim().ToUpperInvariant(), @"\s+", " ");

            if (normalized.StartsWith("UPDATE ", StringComparison.Ordinal) && !normalized.Contains(" WHERE ", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("UPDATE without WHERE is not allowed.");
            }

            if (normalized.StartsWith("DELETE ", StringComparison.Ordinal) && !normalized.Contains(" WHERE ", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("DELETE without WHERE is not allowed.");
            }
        }
    }
}
