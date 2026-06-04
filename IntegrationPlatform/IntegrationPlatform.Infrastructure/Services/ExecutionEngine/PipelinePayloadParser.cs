using System.Text.Json;

namespace IntegrationPlatform.Infrastructure.Services.ExecutionEngine
{
    // Parser do inputData de uma execucao. Usado para validar/parsear o payload ANTES de criar a
    // Execution, evitando execucoes orfas presas em Running quando o input e invalido.
    public static class PipelinePayloadParser
    {
        public static bool TryParse(string? inputData, out Dictionary<string, object> payload)
        {
            payload = new Dictionary<string, object>();

            if (string.IsNullOrWhiteSpace(inputData))
            {
                return true;
            }

            try
            {
                Dictionary<string, object>? parsed = JsonSerializer.Deserialize<Dictionary<string, object>>(inputData);
                if (parsed is null)
                {
                    return true;
                }

                payload = parsed;
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}
