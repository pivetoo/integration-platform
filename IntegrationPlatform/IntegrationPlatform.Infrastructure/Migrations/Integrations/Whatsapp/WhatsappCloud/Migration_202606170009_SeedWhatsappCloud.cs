using FluentMigrator;
using IntegrationPlatform.Domain.ValueObjects;
using IntegrationPlatform.Infrastructure.Migrations.Integrations;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations.Whatsapp.WhatsappCloud
{
    // Integracao Whatsapp: WhatsApp Cloud API (Meta). Idempotente e convergente (no-op onde ja existe).
    [Migration(202606170009)]
    public sealed class Migration_202606170009_SeedWhatsappCloud : IntegrationSeedMigration
    {
        public override void Up()
        {
            SeedIntegration("whatsapp-cloud", "WhatsApp Cloud API (Meta)", "API oficial da Meta para WhatsApp Business (sem intermediario).", "whatsapp", "https://logos.hunter.io/whatsapp.com", supportsWebhook: false);

            SeedAttribute("whatsapp-cloud", "access_token", "Access Token", FieldType.Text, required: true, order: 1, group: "Autenticação", sensitive: true, description: "System User token de longa duracao.");
            SeedAttribute("whatsapp-cloud", "phone_number_id", "Phone Number ID", FieldType.Text, required: true, order: 2, group: "Identificação", description: "ID do numero comercial cadastrado no WABA.");
            SeedAttribute("whatsapp-cloud", "business_account_id", "WABA ID", FieldType.Text, required: true, order: 3, group: "Identificação", description: "ID da conta WhatsApp Business (WABA).");
            SeedAttribute("whatsapp-cloud", "app_secret", "App Secret", FieldType.Text, required: true, order: 4, group: "Autenticação", sensitive: true, description: "App secret usado para validar assinatura X-Hub-Signature dos webhooks.");
            SeedAttribute("whatsapp-cloud", "graph_base_url", "Graph base URL", FieldType.Text, required: true, order: 5, group: "Endpoints", hidden: true, description: "Endpoint Graph. Padrao: https://graph.facebook.com/v20.0.", placeholder: "https://graph.facebook.com/v20.0", defaultValue: "https://graph.facebook.com/v20.0");

            BindContract("whatsapp-cloud", "whatsapp.send");

            SeedApiCall("WhatsApp Cloud: Send Text Message", HttpMethodType.Post, "{{ graph_base_url }}/{{ phone_number_id }}/messages",
                """{"Authorization": "Bearer {{ access_token }}", "Content-Type": "application/json"}""",
                """{"messaging_product":"whatsapp","to":"{{ number }}","type":"text","text":{"body":"{{ message }}"}}""");

            SeedApiCall("WhatsApp Cloud: Send Template Message", HttpMethodType.Post, "{{ graph_base_url }}/{{ phone_number_id }}/messages",
                """{"Authorization": "Bearer {{ access_token }}", "Content-Type": "application/json"}""",
                """{"messaging_product":"whatsapp","to":"{{ number }}","type":"template","template":{"name":"{{ template_name }}","language":{"code":"{{ language_code }}"}}}""");

            SeedApiCall("WhatsApp Cloud: Enviar mensagem de teste", HttpMethodType.Post, "{{ graph_base_url }}/{{ phone_number_id }}/messages",
                """{"Authorization": "Bearer {{ access_token }}", "Content-Type": "application/json"}""",
                """{"messaging_product":"whatsapp","to":"{{ to }}","type":"text","text":{"body":"Teste de integração Mainstay via WhatsApp Cloud API."}}""");

            SeedPipeline("whatsapp-cloud", "whatsapp-cloud-send-message", "Enviar mensagem livre", "POST /{phone_number_id}/messages com tipo text/image/document.", isDefault: true, isTestPipeline: false, contractIdentifier: "whatsapp.send");
            SeedPipeline("whatsapp-cloud", "whatsapp-cloud-send-template", "Enviar template aprovado", "Envia template ja aprovado pela Meta.", isDefault: false, isTestPipeline: false, contractIdentifier: null);
            SeedPipeline("whatsapp-cloud", "whatsapp-cloud-test-connection", "Testar conexao", "Pipeline de validacao de credenciais.", isDefault: false, isTestPipeline: true, contractIdentifier: null);

            SeedStep("whatsapp-cloud-send-message", 1, "Enviar mensagem de texto", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "WhatsApp Cloud: Send Text Message");
            SeedStep("whatsapp-cloud-send-template", 1, "Enviar template aprovado", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "WhatsApp Cloud: Send Template Message");
            SeedStep("whatsapp-cloud-test-connection", 1, "Enviar mensagem de teste", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "WhatsApp Cloud: Enviar mensagem de teste");
        }

        public override void Down()
        {
        }
    }
}
