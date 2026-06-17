using FluentMigrator;
using IntegrationPlatform.Domain.ValueObjects;
using IntegrationPlatform.Infrastructure.Migrations.Integrations;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations.Whatsapp.Evolution
{
    // Integracao Whatsapp: Evolution API (self-hosted, open source). Idempotente e convergente.
    [Migration(202606170013)]
    public sealed class Migration_202606170013_SeedEvolution : IntegrationSeedMigration
    {
        public override void Up()
        {
            SeedIntegration("evolution-api", "Evolution API", "Servidor open source self-hosted para WhatsApp via Baileys.", "whatsapp", "https://raw.githubusercontent.com/evolution-foundation/evolution-api/main/public/hover-evolution.png", supportsWebhook: false);

            SeedAttribute("evolution-api", "base_url", "URL base da Evolution", FieldType.Text, required: true, order: 1, group: "Endpoints", hidden: true, description: "Endpoint da sua instancia self-hosted.", placeholder: "https://evolution.empresa.com");
            SeedAttribute("evolution-api", "api_key", "API Key (Global ou Instance)", FieldType.Text, required: true, order: 2, group: "Autenticação", sensitive: true, description: "API key configurada no servidor.");
            SeedAttribute("evolution-api", "instance_name", "Instance name", FieldType.Text, required: true, order: 3, group: "Identificação", description: "Nome da instancia/sessao na Evolution.");

            BindContract("evolution-api", "whatsapp.send");

            SeedApiCall("Evolution API - Enviar mensagem", HttpMethodType.Post, "{{ base_url }}/message/sendText/{{ instance_name }}",
                """
                {
                    "apikey": "{{ api_key }}",
                    "Content-Type": "application/json"
                }
                """,
                """
                {
                    "number": "{{ number }}",
                    "text": "{{ text }}"
                }
                """);

            SeedApiCall("Evolution API: Enviar mensagem de teste", HttpMethodType.Post, "{{ base_url }}/message/sendText/{{ instance_name }}",
                """{"apikey": "{{ api_key }}", "Content-Type": "application/json"}""",
                """{"number": "{{ to }}", "text": "Teste de integração Mainstay via Evolution API."}""");

            SeedPipeline("evolution-api", "evolution-send-message", "Enviar mensagem", "POST /message/sendText/{instance}.", isDefault: true, isTestPipeline: false, contractIdentifier: "whatsapp.send");
            SeedPipeline("evolution-api", "evolution-api-test-connection", "Testar conexao", "Pipeline de validacao de credenciais.", isDefault: false, isTestPipeline: true, contractIdentifier: null);

            SeedStep("evolution-send-message", 1, "POST /message/sendText/{instance}", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Evolution API - Enviar mensagem");
            SeedStep("evolution-api-test-connection", 1, "Enviar mensagem de teste", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Evolution API: Enviar mensagem de teste");
        }

        public override void Down()
        {
        }
    }
}
