using FluentMigrator;
using IntegrationPlatform.Domain.ValueObjects;
using IntegrationPlatform.Infrastructure.Migrations.Integrations;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations.Email.Mailgun
{
    // Integracao Email: Mailgun. Idempotente e convergente (no-op onde ja existe).
    [Migration(202606170006)]
    public sealed class Migration_202606170006_SeedMailgun : IntegrationSeedMigration
    {
        public override void Up()
        {
            SeedIntegration("mailgun", "Mailgun", "Envio de email via API Mailgun.", "email", "https://logos.hunter.io/mailgun.com", supportsWebhook: false);

            SeedAttribute("mailgun", "api_key", "API Key (Private)", FieldType.Text, required: true, order: 1, group: "Autenticação", sensitive: true, description: "Private API key (key-xxxx) da conta Mailgun.", placeholder: "key-xxxx...");
            SeedAttribute("mailgun", "domain", "Domain (sandbox ou proprio)", FieldType.Text, required: true, order: 2, group: "Endpoints", description: "Dominio configurado no Mailgun para enviar emails.", placeholder: "mg.empresa.com");
            SeedAttribute("mailgun", "region", "Regiao", FieldType.Text, required: true, order: 3, group: "Endpoints", description: "us (default) ou eu. Define endpoint base.", placeholder: "us");
            SeedAttribute("mailgun", "from_email", "Email remetente", FieldType.Text, required: true, order: 4, group: "Remetente", description: "Email com dominio verificado no Mailgun.", placeholder: "no-reply@mg.empresa.com");
            SeedAttribute("mailgun", "from_name", "Nome remetente", FieldType.Text, required: false, order: 5, group: "Remetente", description: "Nome exibido no campo From.", placeholder: "Kanvas");
            SeedAttribute("mailgun", "target_callback_url", "URL de callback", FieldType.Text, required: false, order: 6, group: "Webhook", hidden: true, description: "URL para receber eventos.", placeholder: "https://kanvas.mainstay.com.br/api/EmailEvents/Callback", defaultValue: "https://kanvas.mainstay.com.br/api/EmailEvents/Callback");
            SeedAttribute("mailgun", "target_secret", "Secret do callback", FieldType.Text, required: false, order: 7, group: "Webhook", sensitive: true, hidden: true, description: "Secret enviado no header X-Webhook-Secret.");

            BindContract("mailgun", "email.enviar");

            SeedApiCall("Mailgun - Enviar e-mail", HttpMethodType.Post, "{{ mg_url }}",
                """
                {
                    "Authorization": "{{ mg_auth }}",
                    "Content-Type": "application/x-www-form-urlencoded"
                }
                """,
                "{{ mg_body }}");

            SeedJsFunction("mailgun-montar-requisicao",
                """
                // Entrada:
                //   payload:    { to?, subject?, htmlBody?, textBody?, cc?, bcc?, replyTo? }
                //   attributes: { api_key, domain, region, from_email, from_name? }
                // Saida (result.value):
                //   mg_auth - header Authorization: Basic <base64(api:api_key)>
                //   mg_url  - URL do endpoint baseada na regiao (us / eu)
                //   mg_body - body application/x-www-form-urlencoded

                var apiKey    = attributes.api_key    || '';
                var domain    = attributes.domain     || '';
                var region    = (attributes.region    || 'us').toLowerCase().trim();
                var fromEmail = attributes.from_email || '';
                var fromName  = attributes.from_name  || '';

                var mgAuth = 'Basic ' + btoa('api:' + apiKey);

                var host  = (region === 'eu') ? 'api.eu.mailgun.net' : 'api.mailgun.net';
                var mgUrl = 'https://' + host + '/v3/' + domain + '/messages';

                var from    = fromName ? (fromName + ' <' + fromEmail + '>') : fromEmail;
                var to      = payload.to      || fromEmail;
                var subject = payload.subject || 'Teste de integracao Mainstay';
                var html    = payload.htmlBody || '<p>Este e um email de teste enviado pelo Mainstay para validar a integracao com o Mailgun.</p>';

                var parts = [];
                parts.push('from='    + encodeURIComponent(from));
                parts.push('to='      + encodeURIComponent(to));
                parts.push('subject=' + encodeURIComponent(subject));

                if (payload.htmlBody || !payload.textBody) {
                    parts.push('html=' + encodeURIComponent(html));
                }
                if (payload.textBody) {
                    parts.push('text=' + encodeURIComponent(payload.textBody));
                }
                if (payload.cc) {
                    parts.push('cc=' + encodeURIComponent(payload.cc));
                }
                if (payload.bcc) {
                    parts.push('bcc=' + encodeURIComponent(payload.bcc));
                }
                if (payload.replyTo) {
                    parts.push('h:Reply-To=' + encodeURIComponent(payload.replyTo));
                }

                result.value = { mg_auth: mgAuth, mg_url: mgUrl, mg_body: parts.join('&') };
                """,
                description: "Monta Authorization Basic, URL do endpoint e body form-encoded para Mailgun");

            SeedPipeline("mailgun", "mailgun-enviar-email", "Enviar email", "POST /v3/{domain}/messages form-encoded.", isDefault: true, isTestPipeline: false, contractIdentifier: "email.enviar");
            SeedPipeline("mailgun", "mailgun-testar-conexao", "Testar conexao", "Pipeline de validacao de credenciais.", isDefault: false, isTestPipeline: true, contractIdentifier: null);

            SeedStep("mailgun-enviar-email", 1, "Montar auth e body", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "mailgun-montar-requisicao");
            SeedStep("mailgun-enviar-email", 2, "POST /v3/{domain}/messages", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Mailgun - Enviar e-mail");
            SeedStep("mailgun-testar-conexao", 1, "Montar auth e body (teste)", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "mailgun-montar-requisicao");
            SeedStep("mailgun-testar-conexao", 2, "POST /v3/{domain}/messages (teste)", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Mailgun - Enviar e-mail");
        }

        public override void Down()
        {
        }
    }
}
