using System.Text.Json;

namespace IntegrationPlatform.Infrastructure.Services.Contracts
{
    public enum ContractFieldType
    {
        Unknown,
        String,
        Number,
        Boolean,
        Array,
        Object,
        Enum
    }

    public sealed record ContractField(string Name, ContractFieldType Type, bool IsOptional, IReadOnlyList<string> EnumValues, string Description);

    // Le a convencao informal dos schemas dos contratos de servico (ex.: {"pixKey":"string","note":"string?"}).
    // O primeiro token da descricao define o tipo; o resto e comentario. Token desconhecido nao e
    // rejeitado: o campo so passa a exigir presenca.
    public static class ContractSchemaParser
    {
        public static IReadOnlyList<ContractField> Parse(string? schema)
        {
            if (string.IsNullOrWhiteSpace(schema))
            {
                return [];
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(schema);
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    return [];
                }

                List<ContractField> fields = [];
                foreach (JsonProperty property in document.RootElement.EnumerateObject())
                {
                    string description = property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() ?? string.Empty : string.Empty;
                    fields.Add(property.Value.ValueKind == JsonValueKind.String
                        ? ParseField(property.Name, description)
                        : new ContractField(property.Name, ContractFieldType.Unknown, false, [], description));
                }

                return fields;
            }
            catch (JsonException)
            {
                return [];
            }
        }

        private static ContractField ParseField(string name, string description)
        {
            string trimmed = description.Trim();
            string token = trimmed.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
            bool isOptional = token.EndsWith('?') || trimmed.EndsWith('?');
            string cleanToken = token.TrimEnd('?');

            if (cleanToken.Contains('|'))
            {
                string[] values = cleanToken.Split('|', StringSplitOptions.RemoveEmptyEntries);
                return new ContractField(name, ContractFieldType.Enum, isOptional, values, description);
            }

            ContractFieldType type = cleanToken.ToLowerInvariant() switch
            {
                "string" => ContractFieldType.String,
                "number" => ContractFieldType.Number,
                "bool" or "boolean" => ContractFieldType.Boolean,
                "string[]" or "number[]" => ContractFieldType.Array,
                _ when cleanToken.StartsWith('[') => ContractFieldType.Array,
                _ when cleanToken.StartsWith('{') => ContractFieldType.Object,
                _ => ContractFieldType.Unknown
            };

            return new ContractField(name, type, isOptional, [], description);
        }
    }
}
