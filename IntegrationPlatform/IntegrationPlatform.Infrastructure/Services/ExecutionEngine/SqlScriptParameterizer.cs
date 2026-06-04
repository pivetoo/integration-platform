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

    public static class SqlScriptParameterizer
    {
        private const string TokenInner = @"\s*[\w.\-]+(?:\s*\|\s*\w+)?\s*";

        private static readonly Regex TokenRegex = new(
            $@"'\{{\{{({TokenInner})\}}\}}'|\{{\{{({TokenInner})\}}\}}",
            RegexOptions.Compiled);

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
            Dictionary<string, string> expressionToParameter = new(StringComparer.Ordinal);

            string commandText = TokenRegex.Replace(scriptTemplate, match =>
            {
                string expression = (match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value).Trim();

                if (expressionToParameter.TryGetValue(expression, out string? existingParameter))
                {
                    return existingParameter;
                }

                string parameterName = $"@p{parameters.Count}";
                (string variable, string? filter) = ParseExpression(expression);
                object? value = ResolveValue(variable, filter, stepVariables, payloadData, connectorAttributes);

                parameters.Add(new SqlScriptParameter(parameterName, value));
                expressionToParameter[expression] = parameterName;
                return parameterName;
            });

            return new ParameterizedSql(commandText, parameters);
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
