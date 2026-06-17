using FluentMigrator;
using IntegrationPlatform.Domain.ValueObjects;
using IntegrationPlatform.Infrastructure.Migrations.Integrations;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations.Whatsapp.ZApi
{
    // Integracao Whatsapp: Z-API (provider brasileiro nao-oficial). Idempotente e convergente.
    [Migration(202606170012)]
    public sealed class Migration_202606170012_SeedZApi : IntegrationSeedMigration
    {
        public override void Up()
        {
            SeedIntegration("z-api", "Z-API", "Provider brasileiro nao-oficial baseado em WhatsApp Web (https://z-api.io).", "whatsapp", "https://logos.hunter.io/z-api.io", supportsWebhook: false);

            SeedAttribute("z-api", "instance_id", "Instance ID", FieldType.Text, required: true, order: 1, group: "Identificação", description: "ID da instancia configurada na Z-API.");
            SeedAttribute("z-api", "instance_token", "Instance Token", FieldType.Text, required: true, order: 2, group: "Autenticação", sensitive: true, description: "Token da instancia.");
            SeedAttribute("z-api", "client_token", "Client Token (header)", FieldType.Text, required: true, order: 3, group: "Autenticação", sensitive: true, description: "Client-Token usado no header (Account Security).");
            SeedAttribute("z-api", "base_url", "URL base da API", FieldType.Text, required: true, order: 4, group: "Endpoints", hidden: true, description: "Endpoint padrao: https://api.z-api.io.", placeholder: "https://api.z-api.io", defaultValue: "https://api.z-api.io");

            BindContract("z-api", "whatsapp.send");

            SeedApiCall("Z-API: Send Text Message", HttpMethodType.Post, "{{ base_url }}/instances/{{ instance_id }}/token/{{ instance_token }}/send-text",
                """{"Client-Token": "{{ client_token }}", "Content-Type": "application/json"}""",
                """{"phone": "{{ number }}", "message": "{{ message }}"}""");

            SeedApiCall("Z-API: Enviar mensagem de teste", HttpMethodType.Post, "{{ base_url }}/instances/{{ instance_id }}/token/{{ instance_token }}/send-text",
                """{"Client-Token": "{{ client_token }}", "Content-Type": "application/json"}""",
                """{"phone": "{{ to }}", "message": "Teste de integração Mainstay via Z-API."}""");

            SeedPipeline("z-api", "z-api-send-message", "Enviar mensagem", "POST /instances/{id}/token/{token}/send-text.", isDefault: true, isTestPipeline: false, contractIdentifier: "whatsapp.send");
            SeedPipeline("z-api", "z-api-test-connection", "Testar conexao", "Pipeline de validacao de credenciais.", isDefault: false, isTestPipeline: true, contractIdentifier: null);

            SeedStep("z-api-send-message", 1, "Enviar mensagem de texto", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Z-API: Send Text Message");
            SeedStep("z-api-test-connection", 1, "Enviar mensagem de teste", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Z-API: Enviar mensagem de teste");
        }

        public override void Down()
        {
        }
    }
}
