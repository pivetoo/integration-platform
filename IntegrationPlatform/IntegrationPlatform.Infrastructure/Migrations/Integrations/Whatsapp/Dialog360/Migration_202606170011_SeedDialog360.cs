using FluentMigrator;
using IntegrationPlatform.Domain.ValueObjects;
using IntegrationPlatform.Infrastructure.Migrations.Integrations;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations.Whatsapp.Dialog360
{
    // Integracao Whatsapp: 360dialog (BSP oficial Meta). Idempotente e convergente.
    [Migration(202606170011)]
    public sealed class Migration_202606170011_SeedDialog360 : IntegrationSeedMigration
    {
        public override void Up()
        {
            SeedIntegration("360dialog", "360dialog", "BSP oficial da Meta. API compativel com WhatsApp Business API on-premise/cloud.", "whatsapp", "https://logos.hunter.io/360dialog.com", supportsWebhook: false);

            SeedAttribute("360dialog", "api_key", "API Key (D360-API-KEY)", FieldType.Text, required: true, order: 1, group: "Autenticação", sensitive: true, description: "API key do channel 360dialog.");
            SeedAttribute("360dialog", "base_url", "URL base da API", FieldType.Text, required: true, order: 2, group: "Endpoints", hidden: true, description: "Endpoint waba-v2.360dialog.io ou hub.360dialog.io.", placeholder: "https://waba-v2.360dialog.io", defaultValue: "https://waba-v2.360dialog.io");

            BindContract("360dialog", "whatsapp.send");

            SeedApiCall("360dialog - Enviar mensagem", HttpMethodType.Post, "{{ base_url }}/messages",
                """
                {
                    "D360-API-KEY": "{{ api_key }}",
                    "Content-Type": "application/json"
                }
                """,
                """
                {
                    "messaging_product": "whatsapp",
                    "to": "{{ number }}",
                    "type": "text",
                    "text": {
                        "preview_url": false,
                        "body": "{{ text }}"
                    }
                }
                """);

            SeedApiCall("360dialog - Enviar template", HttpMethodType.Post, "{{ base_url }}/messages",
                """
                {
                    "D360-API-KEY": "{{ api_key }}",
                    "Content-Type": "application/json"
                }
                """,
                """
                {
                    "messaging_product": "whatsapp",
                    "to": "{{ number }}",
                    "type": "template",
                    "template": {
                        "name": "{{ template_name }}",
                        "language": {
                            "code": "{{ language_code }}"
                        }
                    }
                }
                """);

            SeedApiCall("360dialog: Enviar mensagem de teste", HttpMethodType.Post, "{{ base_url }}/messages",
                """{"D360-API-KEY": "{{ api_key }}", "Content-Type": "application/json"}""",
                """{"messaging_product":"whatsapp","to":"{{ to }}","type":"text","text":{"preview_url":false,"body":"Teste de integração Mainstay via 360dialog."}}""");

            SeedPipeline("360dialog", "360dialog-send-message", "Enviar mensagem", "POST /messages com header D360-API-KEY.", isDefault: true, isTestPipeline: false, contractIdentifier: "whatsapp.send");
            SeedPipeline("360dialog", "360dialog-send-template", "Enviar template aprovado", "Envia template HSM aprovado pela Meta.", isDefault: false, isTestPipeline: false, contractIdentifier: null);
            SeedPipeline("360dialog", "360dialog-test-connection", "Testar conexao", "Pipeline de validacao de credenciais.", isDefault: false, isTestPipeline: true, contractIdentifier: null);

            SeedStep("360dialog-send-message", 1, "POST /messages (texto)", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "360dialog - Enviar mensagem");
            SeedStep("360dialog-send-template", 1, "POST /messages (template)", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "360dialog - Enviar template");
            SeedStep("360dialog-test-connection", 1, "Enviar mensagem de teste", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "360dialog: Enviar mensagem de teste");
        }

        public override void Down()
        {
        }
    }
}
