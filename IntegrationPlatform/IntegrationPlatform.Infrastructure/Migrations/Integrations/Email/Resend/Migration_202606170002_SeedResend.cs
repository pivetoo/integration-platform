using FluentMigrator;
using IntegrationPlatform.Domain.ValueObjects;
using IntegrationPlatform.Infrastructure.Migrations.Integrations;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations.Email.Resend
{
    // Integracao Email: Resend. Idempotente e convergente (no-op onde ja existe).
    [Migration(202606170002)]
    public sealed class Migration_202606170002_SeedResend : IntegrationSeedMigration
    {
        public override void Up()
        {
            SeedIntegration("resend", "Resend", "Envio de email via API Resend (https://resend.com).", "email", "https://logos.hunter.io/resend.com", supportsWebhook: false);

            SeedAttribute("resend", "api_key", "API Key", FieldType.Text, required: true, order: 1, group: "Autenticação", sensitive: true, description: "API key (re_xxxx) gerada no Resend.", placeholder: "re_xxxx...");
            SeedAttribute("resend", "base_url", "URL base da API", FieldType.Text, required: true, order: 2, group: "Endpoints", hidden: true, description: "Endpoint padrao: https://api.resend.com.", placeholder: "https://api.resend.com", defaultValue: "https://api.resend.com/emails");
            SeedAttribute("resend", "from_email", "Email remetente", FieldType.Text, required: true, order: 3, group: "Remetente", description: "Email com dominio verificado no Resend.", placeholder: "no-reply@empresa.com");
            SeedAttribute("resend", "from_name", "Nome remetente", FieldType.Text, required: false, order: 4, group: "Remetente", description: "Nome exibido no campo From.", placeholder: "Empresa");

            BindContract("resend", "email.send");

            SeedApiCall("Resend - Enviar email", HttpMethodType.Post, "{{ base_url }}",
                """
                {
                    "Authorization": "Bearer {{ api_key }}",
                    "Content-Type": "application/json"
                }
                """,
                """
                {
                  "from": "{{ from_name }} <{{ from_email }}>",
                  "to": {{ to | json }},
                  "subject": {{ subject | json }},
                  "html": {{ body | json }},
                  "attachments": {{ attachments | json }}
                }
                """);

            SeedApiCall("Resend - Testar conexão", HttpMethodType.Post, "{{ base_url }}",
                """
                {
                    "Authorization": "Bearer {{ api_key }}",
                    "Content-Type": "application/json"
                }
                """,
                """
                {
                    "from": "{{ from_name }} <{{ from_email }}>",
                    "to": ["{{ to }}"],
                    "subject": "Teste de integração Mainstay",
                    "html": "<p>Este é um email de teste enviado pelo Mainstay para validar a integração com o Resend.</p>"
                }
                """);

            SeedJsFunction("resend-prepare-attachments",
                """
                const items = Array.isArray(payload.attachments) ? payload.attachments : [];
                result.value = {
                  attachments: items.map(function(a) { return { filename: a.filename, path: a.url || a.path }; })
                };
                """,
                description: "Converte attachments do payload canonico (filename,url) para o formato Resend (filename,path).");

            SeedPipeline("resend", "resend-send-email", "Enviar email", "POST /emails com body JSON.", isDefault: true, isTestPipeline: false, contractIdentifier: "email.send");
            SeedPipeline("resend", "resend-test-connection", "Testar conexao", "Pipeline executado pelo botao Testar do AgencyCampaign para validar credenciais. Em providers de email/whatsapp, envia mensagem de teste; em pagamento, valida sandbox.", isDefault: false, isTestPipeline: true, contractIdentifier: null);

            SeedStep("resend-send-email", 0, "Adapt attachments para Resend", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "resend-prepare-attachments", ignoreOnResponse: true);
            SeedStep("resend-send-email", 1, "POST /emails", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Resend - Enviar email");
            SeedStep("resend-test-connection", 1, "POST /emails", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Resend - Testar conexão");
        }

        public override void Down()
        {
        }
    }
}
