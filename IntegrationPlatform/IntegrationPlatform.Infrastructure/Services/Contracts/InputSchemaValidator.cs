using System.Text.Json;

namespace IntegrationPlatform.Infrastructure.Services.Contracts
{
    // Confere o JSON de entrada de um servico contra os campos do InputSchema. Devolve as violacoes na
    // ordem dos campos; lista vazia quando valido ou quando a entrada nao e um objeto JSON (esse caso
    // segue para o erro que o motor ja emite). Campos extras sao aceitos.
    public static class InputSchemaValidator
    {
        public static IReadOnlyList<string> Validate(IReadOnlyList<ContractField> fields, string? inputData)
        {
            if (fields.Count == 0)
            {
                return [];
            }

            string json = string.IsNullOrWhiteSpace(inputData) ? "{}" : inputData;

            try
            {
                using JsonDocument document = JsonDocument.Parse(json);
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    return [];
                }

                List<string> violations = [];
                foreach (ContractField field in fields)
                {
                    string? violation = CheckField(field, document.RootElement);
                    if (violation is not null)
                    {
                        violations.Add(violation);
                    }
                }

                return violations;
            }
            catch (JsonException)
            {
                return [];
            }
        }

        private static string? CheckField(ContractField field, JsonElement root)
        {
            if (!root.TryGetProperty(field.Name, out JsonElement value) || value.ValueKind == JsonValueKind.Null)
            {
                return field.IsOptional ? null : $"{field.Name}: required";
            }

            return field.Type switch
            {
                ContractFieldType.String => Expect(field, value.ValueKind == JsonValueKind.String, "string"),
                ContractFieldType.Number => Expect(field, value.ValueKind == JsonValueKind.Number, "number"),
                ContractFieldType.Boolean => Expect(field, value.ValueKind is JsonValueKind.True or JsonValueKind.False, "bool"),
                ContractFieldType.Array => Expect(field, value.ValueKind == JsonValueKind.Array, "array"),
                ContractFieldType.Object => Expect(field, value.ValueKind == JsonValueKind.Object, "object"),
                ContractFieldType.Enum => CheckEnum(field, value),
                _ => null
            };
        }

        private static string? Expect(ContractField field, bool valid, string typeName)
        {
            return valid ? null : $"{field.Name}: expected {typeName}";
        }

        private static string? CheckEnum(ContractField field, JsonElement value)
        {
            bool valid = value.ValueKind == JsonValueKind.String
                && field.EnumValues.Any(option => string.Equals(option, value.GetString(), StringComparison.OrdinalIgnoreCase));

            return valid ? null : $"{field.Name}: expected one of {string.Join('|', field.EnumValues)}";
        }
    }
}
