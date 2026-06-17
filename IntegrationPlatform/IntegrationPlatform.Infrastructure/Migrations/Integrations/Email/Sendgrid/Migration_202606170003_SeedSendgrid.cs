using FluentMigrator;
using IntegrationPlatform.Domain.ValueObjects;
using IntegrationPlatform.Infrastructure.Migrations.Integrations;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations.Email.Sendgrid
{
    // Integracao Email: SendGrid. Idempotente e convergente (no-op onde ja existe).
    [Migration(202606170003)]
    public sealed class Migration_202606170003_SeedSendgrid : IntegrationSeedMigration
    {
        public override void Up()
        {
            SeedIntegration("sendgrid", "SendGrid", "Envio de email via API SendGrid (Twilio).", "email", "https://logos.hunter.io/sendgrid.com", supportsWebhook: false);

            SeedAttribute("sendgrid", "api_key", "API Key", FieldType.Text, required: true, order: 1, group: "Autenticação", sensitive: true, description: "API key gerada em Settings > API Keys.", placeholder: "SG.xxxxx...");
            SeedAttribute("sendgrid", "base_url", "URL base da API", FieldType.Text, required: true, order: 2, group: "Endpoints", hidden: true, description: "Endpoint da API. Padrao: https://api.sendgrid.com.", placeholder: "https://api.sendgrid.com", defaultValue: "https://api.sendgrid.com");
            SeedAttribute("sendgrid", "from_email", "Email remetente", FieldType.Text, required: true, order: 3, group: "Remetente", description: "Email verificado no SendGrid.", placeholder: "no-reply@empresa.com");
            SeedAttribute("sendgrid", "from_name", "Nome remetente", FieldType.Text, required: false, order: 4, group: "Remetente", description: "Nome exibido no campo From.", placeholder: "Empresa");

            BindContract("sendgrid", "email.send");

            SeedApiCall("SendGrid - Enviar e-mail", HttpMethodType.Post, "{{ base_url }}/v3/mail/send",
                """
                {
                    "Authorization": "Bearer {{ api_key }}",
                    "Content-Type": "application/json"
                }
                """,
                """
                {
                    "from": {
                        "email": "{{ from_email }}",
                        "name": "{{ from_name }}"
                    },
                    "personalizations": [
                        {
                            "to": [{ "email": "{{ to }}" }],
                            "subject": "{{ subject }}"
                        }
                    ],
                    "content": [
                        {
                            "type": "text/html",
                            "value": "{{ html_body }}"
                        }
                    ]
                }
                """);

            SeedApiCall("SendGrid - Testar conexão", HttpMethodType.Post, "{{ base_url }}/v3/mail/send",
                """
                {
                    "Authorization": "Bearer {{ api_key }}",
                    "Content-Type": "application/json"
                }
                """,
                """
                {
                    "from": {
                        "email": "{{ from_email }}",
                        "name": "{{ from_name }}"
                    },
                    "personalizations": [
                        {
                            "to": [{ "email": "{{ to }}" }],
                            "subject": "Teste de integração Mainstay"
                        }
                    ],
                    "content": [
                        {
                            "type": "text/html",
                            "value": "<p>Este é um email de teste enviado pelo Mainstay para validar a integração com o SendGrid.</p>"
                        }
                    ]
                }
                """);

            SeedPipeline("sendgrid", "sendgrid-send-email", "Enviar email", "POST /v3/mail/send com body em JSON.", isDefault: true, isTestPipeline: false, contractIdentifier: "email.send");
            SeedPipeline("sendgrid", "sendgrid-test-connection", "Testar conexao", "Pipeline executado pelo botao Testar do AgencyCampaign para validar credenciais.", isDefault: false, isTestPipeline: true, contractIdentifier: null);

            SeedStep("sendgrid-send-email", 1, "POST /v3/mail/send", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "SendGrid - Enviar e-mail");
            SeedStep("sendgrid-test-connection", 1, "POST /v3/mail/send (teste)", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "SendGrid - Testar conexão");
        }

        public override void Down()
        {
        }
    }
}
