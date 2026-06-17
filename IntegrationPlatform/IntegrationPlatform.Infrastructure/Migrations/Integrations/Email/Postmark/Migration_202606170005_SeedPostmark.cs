using FluentMigrator;
using IntegrationPlatform.Domain.ValueObjects;
using IntegrationPlatform.Infrastructure.Migrations.Integrations;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations.Email.Postmark
{
    // Integracao Email: Postmark. Idempotente e convergente (no-op onde ja existe).
    [Migration(202606170005)]
    public sealed class Migration_202606170005_SeedPostmark : IntegrationSeedMigration
    {
        public override void Up()
        {
            SeedIntegration("postmark", "Postmark", "Envio de email transacional via API Postmark.", "email", "https://logos.hunter.io/postmarkapp.com", supportsWebhook: false);

            SeedAttribute("postmark", "server_token", "Server Token", FieldType.Text, required: true, order: 1, group: "Autenticação", sensitive: true, description: "Token do server (Server > API Tokens). Cada server tem o seu.");
            SeedAttribute("postmark", "base_url", "URL base da API", FieldType.Text, required: true, order: 2, group: "Endpoints", hidden: true, description: "Endpoint padrao: https://api.postmarkapp.com.", placeholder: "https://api.postmarkapp.com", defaultValue: "https://api.postmarkapp.com");
            SeedAttribute("postmark", "from_email", "Email remetente", FieldType.Text, required: true, order: 3, group: "Remetente", description: "Endereco com Sender Signature aprovada.", placeholder: "no-reply@empresa.com");
            SeedAttribute("postmark", "from_name", "Nome remetente", FieldType.Text, required: false, order: 4, group: "Remetente", description: "Nome exibido no campo From.", placeholder: "Kanvas");
            SeedAttribute("postmark", "target_callback_url", "URL de callback", FieldType.Text, required: false, order: 5, group: "Webhook", hidden: true, description: "URL para receber webhooks de eventos.", placeholder: "https://kanvas.mainstay.com.br/api/EmailEvents/Callback", defaultValue: "https://kanvas.mainstay.com.br/api/EmailEvents/Callback");

            BindContract("postmark", "email.send");

            SeedApiCall("Postmark - Enviar e-mail", HttpMethodType.Post, "{{ base_url }}/email",
                """
                {
                    "X-Postmark-Server-Token": "{{ server_token }}",
                    "Content-Type": "application/json",
                    "Accept": "application/json"
                }
                """,
                """
                {
                    "From": "{{ from_name }} <{{ from_email }}>",
                    "To": "{{ to }}",
                    "Subject": "{{ subject }}",
                    "HtmlBody": "{{ html_body }}",
                    "TextBody": "{{ text_body }}"
                }
                """);

            SeedApiCall("Postmark - Testar conexão", HttpMethodType.Post, "{{ base_url }}/email",
                """
                {
                    "X-Postmark-Server-Token": "{{ server_token }}",
                    "Content-Type": "application/json",
                    "Accept": "application/json"
                }
                """,
                """
                {
                    "From": "{{ from_name }} <{{ from_email }}>",
                    "To": "{{ to }}",
                    "Subject": "Teste de integração Mainstay",
                    "HtmlBody": "<p>Este é um email de teste enviado pelo Mainstay para validar a integração com o Postmark.</p>"
                }
                """);

            SeedPipeline("postmark", "postmark-send-email", "Enviar email", "POST /email com body em JSON.", isDefault: true, isTestPipeline: false, contractIdentifier: "email.send");
            SeedPipeline("postmark", "postmark-test-connection", "Testar conexao", "Pipeline de validacao de credenciais.", isDefault: false, isTestPipeline: true, contractIdentifier: null);

            SeedStep("postmark-send-email", 1, "POST /email", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Postmark - Enviar e-mail");
            SeedStep("postmark-test-connection", 1, "POST /email (teste)", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Postmark - Testar conexão");
        }

        public override void Down()
        {
        }
    }
}
