using IntegrationPlatform.Application.Localization;
using Microsoft.Extensions.Localization;
using Microsoft.Data.SqlClient;
using System.Text.RegularExpressions;

namespace IntegrationPlatform.Infrastructure.Services.ExecutionEngine
{
    internal sealed class SqlServerExecutor : IDatabaseExecutor
    {
        private readonly IStringLocalizer<IntegrationPlatformResource> Localizer;

        public SqlServerExecutor(IStringLocalizer<IntegrationPlatformResource> localizer)
        {
            Localizer = localizer;
        }

        public async Task<object> ExecuteQueryAsync(string connectionString, string query, IReadOnlyList<SqlScriptParameter> parameters, int timeoutSeconds = 60, CancellationToken cancellationToken = default)
        {
            ValidateQuery(query);

            await using SqlConnection connection = new(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using SqlCommand command = new(query, connection)
            {
                CommandTimeout = timeoutSeconds
            };

            foreach (SqlScriptParameter parameter in parameters)
            {
                command.Parameters.AddWithValue(parameter.Name, parameter.Value ?? DBNull.Value);
            }

            string trimmedQuery = query.Trim().ToUpperInvariant();
            bool isSelect = trimmedQuery.StartsWith("SELECT", StringComparison.Ordinal) ||
                            trimmedQuery.StartsWith("WITH", StringComparison.Ordinal);

            if (isSelect)
            {
                List<Dictionary<string, object?>> results = [];

                await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
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

        private void ValidateQuery(string query)
        {
            string normalized = Regex.Replace(query.Trim().ToUpperInvariant(), @"\s+", " ");

            if (normalized.StartsWith("UPDATE ", StringComparison.Ordinal) && !normalized.Contains(" WHERE ", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("database.query.updateWithoutWhereNotAllowed");
            }

            if (normalized.StartsWith("DELETE ", StringComparison.Ordinal) && !normalized.Contains(" WHERE ", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("database.query.deleteWithoutWhereNotAllowed");
            }
        }
    }
}
