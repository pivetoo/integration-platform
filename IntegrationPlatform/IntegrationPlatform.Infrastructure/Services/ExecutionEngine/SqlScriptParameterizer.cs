using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace IntegrationPlatform.Infrastructure.Services.ExecutionEngine
{
    public sealed record SqlScriptParameter(string Name, object? Value);

    public sealed class ParameterizedSql
    {
        public ParameterizedSql(string commandText, IReadOnlyList<SqlScriptParameter> parameters)
        {
            CommandText = commandText;
            Parameters = parameters;
        }

        public string CommandText { get; }

        public IReadOnlyList<SqlScriptParameter> Parameters { get; }
    }

    /// <summary>
    /// Converte um script SQL com tokens {{ var }} em SQL parametrizado (@p0, @p1, ...),
    /// vinculando os valores resolvidos como parametros ADO.NET em vez de concatena-los no
    /// texto do comando. Dados de origem externa (payload do webhook, variaveis de step)
    /// nunca alcancam o texto do SQL: viram sempre valor de parametro.
    /// </summary>
    public static class SqlScriptParameterizer
    {
        private const string TokenInner = @"\s*[\w.\-]+(?:\s*\|\s*\w+)?\s*";

        private static readonly Regex TokenRegex = new($@"\{{\{{({TokenInner})\}}\}}", RegexOptions.Compiled);

        // Casa um literal single-quoted completo (tratando aspas escapadas '') OU um token nao-aspado.
        private static readonly Regex SegmentRegex = new($@"'(?:[^']|'')*'|\{{\{{({TokenInner})\}}\}}", RegexOptions.Compiled);

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public static ParameterizedSql Build(
            string? scriptTemplate,
            Dictionary<string, object> stepVariables,
            Dictionary<string, object> payloadData,
            Dictionary<string, string> connectorAttributes)
        {
            if (string.IsNullOrWhiteSpace(scriptTemplate))
            {
                return new ParameterizedSql(scriptTemplate ?? string.Empty, []);
            }

            List<SqlScriptParameter> parameters = [];
            Dictionary<string, string> valueKeyToParameter = new(StringComparer.Ordinal);

            string AddParameter(object? value)
            {
                string key = ValueKey(value);
                if (valueKeyToParameter.TryGetValue(key, out string? existing))
                {
                    return existing;
                }

                string parameterName = $"@p{parameters.Count}";
                parameters.Add(new SqlScriptParameter(parameterName, value));
                valueKeyToParameter[key] = parameterName;
                return parameterName;
            }

            string commandText = SegmentRegex.Replace(scriptTemplate, match =>
            {
                bool isLiteral = match.Value.Length > 0 && match.Value[0] == '\'';

                if (isLiteral)
                {
                    string innerRaw = match.Value.Substring(1, match.Value.Length - 2);
                    string innerUnescaped = innerRaw.Replace("''", "'");

                    if (!TokenRegex.IsMatch(innerUnescaped))
                    {
                        return match.Value;
                    }

                    string assembledValue = TokenRegex.Replace(innerUnescaped, inner =>
                    {
                        (string variable, string? filter) = ParseExpression(inner.Groups[1].Value.Trim());
                        return ResolveString(variable, filter, stepVariables, payloadData, connectorAttributes);
                    });

                    return AddParameter(assembledValue);
                }

                (string tokenVariable, string? tokenFilter) = ParseExpression(match.Groups[1].Value.Trim());
                object? value = ResolveValue(tokenVariable, tokenFilter, stepVariables, payloadData, connectorAttributes);
                return AddParameter(value);
            });

            return new ParameterizedSql(commandText, parameters);
        }

        private static string ValueKey(object? value)
        {
            if (value is null)
            {
                return "\0null";
            }

            return value.GetType().FullName + "" + Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        private static (string variable, string? filter) ParseExpression(string expression)
        {
            int pipeIndex = expression.IndexOf('|');
            if (pipeIndex < 0)
            {
                return (expression.Trim(), null);
            }

            string variable = expression[..pipeIndex].Trim();
            string filter = expression[(pipeIndex + 1)..].Trim();
            return (variable, filter);
        }

        // Resolve um token solto (fora de literal) para seu valor tipado, ligado como parametro.
        private static object? ResolveValue(
            string variable,
            string? filter,
            Dictionary<string, object> stepVariables,
            Dictionary<string, object> payloadData,
            Dictionary<string, string> connectorAttributes)
        {
            if (TryResolveRaw(variable, stepVariables, payloadData, connectorAttributes, out object? raw))
            {
                if (filter == "json")
                {
                    return JsonSerializer.Serialize(raw, JsonOptions);
                }

                return Normalize(raw);
            }

            return filter == "json" ? "null" : string.Empty;
        }

        // Resolve um token dentro de um literal para sua forma string, para montar o valor do literal.
        private static string ResolveString(
            string variable,
            string? filter,
            Dictionary<string, object> stepVariables,
            Dictionary<string, object> payloadData,
            Dictionary<string, string> connectorAttributes)
        {
            if (TryResolveRaw(variable, stepVariables, payloadData, connectorAttributes, out object? raw))
            {
                if (filter == "json")
                {
                    return JsonSerializer.Serialize(raw, JsonOptions);
                }

                return ConvertToString(raw);
            }

            return filter == "json" ? "null" : string.Empty;
        }

        private static bool TryResolveRaw(
            string variable,
            Dictionary<string, object> stepVariables,
            Dictionary<string, object> payloadData,
            Dictionary<string, string> connectorAttributes,
            out object? value)
        {
            if (stepVariables.TryGetValue(variable, out object? stepValue) ||
                TryGetValueIgnoreCase(stepVariables, variable, out stepValue))
            {
                value = stepValue;
                return true;
            }

            object? payloadValue = ResolveDottedPath(payloadData, variable);
            if (payloadValue is not null)
            {
                value = payloadValue;
                return true;
            }

            if (connectorAttributes.TryGetValue(variable, out string? attributeValue) ||
                TryGetValueIgnoreCase(connectorAttributes, variable, out attributeValue))
            {
                value = attributeValue;
                return true;
            }

            value = null;
            return false;
        }

        private static object? Normalize(object? value)
        {
            if (value is JsonElement jsonElement)
            {
                return jsonElement.ValueKind switch
                {
                    JsonValueKind.String => jsonElement.GetString(),
                    JsonValueKind.Number when jsonElement.TryGetInt64(out long integerValue) => integerValue,
                    JsonValueKind.Number when jsonElement.TryGetDouble(out double doubleValue) => doubleValue,
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null,
                    _ => jsonElement.GetRawText()
                };
            }

            return value;
        }

        private static string ConvertToString(object? value)
        {
            if (value is null)
            {
                return string.Empty;
            }

            if (value is JsonElement jsonElement)
            {
                return jsonElement.ValueKind switch
                {
                    JsonValueKind.String => jsonElement.GetString() ?? string.Empty,
                    JsonValueKind.Number => jsonElement.GetRawText(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    JsonValueKind.Null => string.Empty,
                    _ => jsonElement.GetRawText()
                };
            }

            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        private static object? ResolveDottedPath(Dictionary<string, object> data, string path)
        {
            string[] parts = path.Split('.');
            object? current = data;

            foreach (string part in parts)
            {
                if (current is null)
                {
                    return null;
                }

                if (current is Dictionary<string, object> dictionary)
                {
                    if (!dictionary.TryGetValue(part, out current))
                    {
                        return null;
                    }
                }
                else if (current is JsonElement jsonElement)
                {
                    if (jsonElement.ValueKind == JsonValueKind.Object &&
                        jsonElement.TryGetProperty(part, out JsonElement property))
                    {
                        current = property;
                    }
                    else if (jsonElement.ValueKind == JsonValueKind.Array && int.TryParse(part, out int index) && index >= 0 && index < jsonElement.GetArrayLength())
                    {
                        current = jsonElement[index];
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }

            return current;
        }

        private static bool TryGetValueIgnoreCase<T>(Dictionary<string, T> source, string key, out T? value)
        {
            foreach ((string sourceKey, T sourceValue) in source)
            {
                if (string.Equals(sourceKey, key, StringComparison.OrdinalIgnoreCase))
                {
                    value = sourceValue;
                    return true;
                }
            }

            value = default;
            return false;
        }
    }
}
