using Archon.Core.Entities;

namespace IntegrationPlatform.Domain.Entities
{
    public class Connector : Entity
    {
        private readonly List<ConnectorAttributeValue> attributeValues = [];
        private readonly List<Execution> executions = [];
        private readonly List<ProcessingQueue> processingQueues = [];
        private readonly List<Reference> references = [];
        private readonly List<PipelineRoutine> routines = [];

        public string? SystemApplicationId { get; private set; }

        public long IntegrationId { get; private set; }

        public Integration Integration { get; private set; } = null!;

        public string Name { get; private set; } = string.Empty;

        public bool IsActive { get; private set; } = true;

        // Conta padrao da CATEGORIA (email, whatsapp...): e a que os sistemas consumidores usam quando o
        // fluxo nao escolhe uma conta especifica. No maximo uma por categoria (ConnectorService garante).
        public bool IsDefault { get; private set; }

        public string? WebhookToken { get; private set; }

        public string? CallbackUrl { get; private set; }

        public string? CallbackToken { get; private set; }

        public IReadOnlyCollection<ConnectorAttributeValue> AttributeValues => attributeValues.AsReadOnly();

        public IReadOnlyCollection<Execution> Executions => executions.AsReadOnly();

        public IReadOnlyCollection<ProcessingQueue> ProcessingQueues => processingQueues.AsReadOnly();

        public IReadOnlyCollection<Reference> References => references.AsReadOnly();

        public IReadOnlyCollection<PipelineRoutine> Routines => routines.AsReadOnly();

        private Connector()
        {
        }

        public Connector(long integrationId, string name, string? systemApplicationId = null)
        {
            if (integrationId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(integrationId));
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            IntegrationId = integrationId;
            Name = name.Trim();
            SystemApplicationId = systemApplicationId?.Trim();
            WebhookToken = GenerateWebhookToken();
        }

        public void Update(long integrationId, string name, string? systemApplicationId, bool isActive)
        {
            if (integrationId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(integrationId));
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            IntegrationId = integrationId;
            Name = name.Trim();
            SystemApplicationId = systemApplicationId?.Trim();
            IsActive = isActive;
        }

        public void SetCallback(string? callbackUrl, string? callbackToken)
        {
            CallbackUrl = string.IsNullOrWhiteSpace(callbackUrl) ? null : callbackUrl.Trim();
            CallbackToken = string.IsNullOrWhiteSpace(callbackToken) ? null : callbackToken.Trim();
        }

        public void EnsureWebhookToken()
        {
            if (string.IsNullOrWhiteSpace(WebhookToken))
            {
                WebhookToken = GenerateWebhookToken();
            }
        }

        public void SetDefault(bool isDefault)
        {
            IsDefault = isDefault;
        }

        public void RegenerateWebhookToken()
        {
            WebhookToken = GenerateWebhookToken();
        }

        private static string GenerateWebhookToken()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}
