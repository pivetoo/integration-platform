using FluentMigrator;
using IntegrationPlatform.Domain.ValueObjects;
using IntegrationPlatform.Infrastructure.Migrations.Integrations;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations.Email.Brevo
{
    // Integracao Email: Brevo. Idempotente e convergente (no-op onde ja existe).
    [Migration(202606170004)]
    public sealed class Migration_202606170004_SeedBrevo : IntegrationSeedMigration
    {
        public override void Up()
        {
            SeedIntegration("brevo", "Brevo", "Plataforma de email transacional com alta entregabilidade.", "email", "https://companieslogo.com/img/orig/brevo_BIG-b86dd8e8.svg?t=1720244494", supportsWebhook: false);

            SeedAttribute("brevo", "api_key", "API Key", FieldType.Text, required: true, order: 1, group: "Autenticação", sensitive: true, description: "Chave gerada em Settings > API Keys.", placeholder: "xkeysib-...");
            SeedAttribute("brevo", "base_url", "URL base da API", FieldType.Text, required: true, order: 2, group: "Endpoints", hidden: true, description: "Endpoint da API Brevo.", placeholder: "https://api.brevo.com", defaultValue: "https://api.brevo.com");
            SeedAttribute("brevo", "from_email", "Email remetente", FieldType.Text, required: true, order: 3, group: "Remetente", description: "Email do remetente (dominio autenticado no Brevo).", placeholder: "noreply@empresa.com");
            SeedAttribute("brevo", "from_name", "Nome remetente", FieldType.Text, required: false, order: 4, group: "Remetente", description: "Nome exibido no campo From.", placeholder: "Mainstay");

            BindContract("brevo", "email.send");

            SeedApiCall("Brevo - Enviar e-mail", HttpMethodType.Post, "{{ base_url }}/v3/smtp/email",
                """{"api-key": "{{ api_key }}", "content-type": "application/json", "accept": "application/json"}""",
                """{"sender": {"name": "{{ from_name }}", "email": "{{ from_email }}"}, "to": [{"email": "{{ to }}"}], "subject": "{{ subject }}", "htmlContent": "{{ html_body }}"}""");

            SeedApiCall("Brevo - Testar conexao", HttpMethodType.Post, "{{ base_url }}/v3/smtp/email",
                """{"api-key": "{{ api_key }}", "content-type": "application/json", "accept": "application/json"}""",
                """{"sender": {"name": "{{ from_name }}", "email": "{{ from_email }}"}, "to": [{"email": "{{ to }}"}], "subject": "Teste de integracao Mainstay", "htmlContent": "<p>Este e um email de teste enviado pelo Mainstay para validar a integracao com o Brevo.</p>"}""");

            SeedPipeline("brevo", "brevo-send-email", "Enviar email", "POST /v3/smtp/email com body em JSON.", isDefault: true, isTestPipeline: false, contractIdentifier: "email.send");
            SeedPipeline("brevo", "brevo-test-connection", "Testar conexao", "Pipeline de validacao de credenciais.", isDefault: false, isTestPipeline: true, contractIdentifier: null);

            SeedStep("brevo-send-email", 1, "POST /v3/smtp/email", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Brevo - Enviar e-mail");
            SeedStep("brevo-test-connection", 1, "POST /v3/smtp/email (teste)", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Brevo - Testar conexao");
        }

        public override void Down()
        {
        }
    }
}
