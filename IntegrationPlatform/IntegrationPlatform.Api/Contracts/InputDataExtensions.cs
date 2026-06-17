using System.Text.Json;

namespace IntegrationPlatform.Api.Contracts
{
    public static class InputDataExtensions
    {
        // Converte o inputData recebido (objeto JSON novo, ou string JSON escapada legada) no texto cru
        // que o motor de pipeline interpola/persiste. Mantem compatibilidade durante a transicao dos
        // consumidores: ValueKind.String -> texto interno (legado); objeto/array -> raw JSON (novo).
        public static string? ToRawInputData(this JsonElement? element)
        {
            if (element is not JsonElement value)
            {
                return null;
            }

            return value.ValueKind switch
            {
                JsonValueKind.Undefined or JsonValueKind.Null => null,
                JsonValueKind.String => value.GetString(),
                _ => value.GetRawText()
            };
        }
    }
}
