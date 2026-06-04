using IntegrationPlatform.Domain.Entities;

namespace IntegrationPlatform.Application.Models
{
    public class PipelineExecutionContext
    {
        public Connector Connector { get; set; } = null!;

        public Pipeline Pipeline { get; set; } = null!;

        public Execution Execution { get; set; } = null!;

        public Dictionary<string, string> ConnectorAttributes { get; set; } = [];

        // Campos de ConnectorAttributes marcados como IsSensitive (senhas, API keys). Usados para NAO
        // injetar segredos no escopo do passo JavaScript do usuario.
        public HashSet<string> SensitiveAttributeFields { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, object> StepVariables { get; set; } = [];

        public Dictionary<string, object> PayloadData { get; set; } = [];

        public bool HasError { get; set; }

        public string? LastError { get; set; }
    }
}
