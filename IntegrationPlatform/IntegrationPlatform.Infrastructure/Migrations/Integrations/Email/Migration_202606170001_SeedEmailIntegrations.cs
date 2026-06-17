using FluentMigrator;
using IntegrationPlatform.Domain.ValueObjects;
using IntegrationPlatform.Infrastructure.Migrations.Integrations;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations.Email
{
    // Seed do modulo Email: categoria 'email' + contract 'email.send' + 6 integracoes
    // (resend, sendgrid, brevo, postmark, mailgun, smtp) com atributos, pipelines, steps,
    // apicalls e funcoes JS. Idempotente e convergente (no-op onde ja existe, ex.: empresa_18).
    [Migration(202606170001)]
    public sealed class Migration_202606170001_SeedEmailIntegrations : IntegrationSeedMigration
    {
        public override void Up()
        {
            SeedCategory("email", "Email", "Provedores de envio de e-mail transacional.");

            SeedContract(
                identifier: "email.send",
                name: "Envio de email",
                description: "Envia um e-mail transacional para um ou mais destinatarios.",
                categoryIdentifier: "email",
                inputSchema: """
                {
                  "to": "string[] (obrigatorio)",
                  "cc": "string[]?",
                  "bcc": "string[]?",
                  "subject": "string (obrigatorio)",
                  "body": "string (obrigatorio)",
                  "isHtml": "bool",
                  "attachments": "[{filename, contentType, contentBase64 | url}]?",
                  "replyTo": "string?",
                  "from": "{email, name}?"
                }
                """,
                outputSchema: """
                {
                  "providerMessageId": "string",
                  "status": "accepted | rejected",
                  "error": "{code, message}?"
                }
                """,
                hasCallback: false,
                callbackSchema: null);

            SeedResend();
            SeedSendGrid();
            SeedBrevo();
            SeedPostmark();
            SeedMailgun();
            SeedSmtp();
        }

        public override void Down()
        {
            // Seed de convergencia de catalogo; Down nao reverte (connectors de tenant referenciam
            // estas integracoes e a remocao quebraria FK). Para retirar uma integracao, criar migration propria.
        }

        private void SeedResend()
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

        private void SeedSendGrid()
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

        private void SeedBrevo()
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

        private void SeedPostmark()
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

        private void SeedMailgun()
        {
            SeedIntegration("mailgun", "Mailgun", "Envio de email via API Mailgun.", "email", "https://logos.hunter.io/mailgun.com", supportsWebhook: false);

            SeedAttribute("mailgun", "api_key", "API Key (Private)", FieldType.Text, required: true, order: 1, group: "Autenticação", sensitive: true, description: "Private API key (key-xxxx) da conta Mailgun.", placeholder: "key-xxxx...");
            SeedAttribute("mailgun", "domain", "Domain (sandbox ou proprio)", FieldType.Text, required: true, order: 2, group: "Endpoints", description: "Dominio configurado no Mailgun para enviar emails.", placeholder: "mg.empresa.com");
            SeedAttribute("mailgun", "region", "Regiao", FieldType.Text, required: true, order: 3, group: "Endpoints", description: "us (default) ou eu. Define endpoint base.", placeholder: "us");
            SeedAttribute("mailgun", "from_email", "Email remetente", FieldType.Text, required: true, order: 4, group: "Remetente", description: "Email com dominio verificado no Mailgun.", placeholder: "no-reply@mg.empresa.com");
            SeedAttribute("mailgun", "from_name", "Nome remetente", FieldType.Text, required: false, order: 5, group: "Remetente", description: "Nome exibido no campo From.", placeholder: "Kanvas");
            SeedAttribute("mailgun", "target_callback_url", "URL de callback", FieldType.Text, required: false, order: 6, group: "Webhook", hidden: true, description: "URL para receber eventos.", placeholder: "https://kanvas.mainstay.com.br/api/EmailEvents/Callback", defaultValue: "https://kanvas.mainstay.com.br/api/EmailEvents/Callback");
            SeedAttribute("mailgun", "target_secret", "Secret do callback", FieldType.Text, required: false, order: 7, group: "Webhook", sensitive: true, hidden: true, description: "Secret enviado no header X-Webhook-Secret.");

            BindContract("mailgun", "email.send");

            SeedApiCall("Mailgun - Enviar e-mail", HttpMethodType.Post, "{{ mg_url }}",
                """
                {
                    "Authorization": "{{ mg_auth }}",
                    "Content-Type": "application/x-www-form-urlencoded"
                }
                """,
                "{{ mg_body }}");

            SeedJsFunction("mailgun-build-request",
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

            SeedPipeline("mailgun", "mailgun-send-email", "Enviar email", "POST /v3/{domain}/messages form-encoded.", isDefault: true, isTestPipeline: false, contractIdentifier: "email.send");
            SeedPipeline("mailgun", "mailgun-test-connection", "Testar conexao", "Pipeline de validacao de credenciais.", isDefault: false, isTestPipeline: true, contractIdentifier: null);

            SeedStep("mailgun-send-email", 1, "Montar auth e body", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "mailgun-build-request");
            SeedStep("mailgun-send-email", 2, "POST /v3/{domain}/messages", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Mailgun - Enviar e-mail");
            SeedStep("mailgun-test-connection", 1, "Montar auth e body (teste)", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "mailgun-build-request");
            SeedStep("mailgun-test-connection", 2, "POST /v3/{domain}/messages (teste)", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Mailgun - Enviar e-mail");
        }

        private void SeedSmtp()
        {
            SeedIntegration("smtp", "SMTP", "Envio de email via servidor SMTP generico (Gmail, Outlook, Postfix, AWS SES SMTP, etc).", "email", "https://logos.hunter.io/minutemailer.com", supportsWebhook: false);

            SeedAttribute("smtp", "host", "Host SMTP", FieldType.Text, required: true, order: 1, group: "Servidor", description: "Endereco do servidor SMTP.", placeholder: "smtp.example.com");
            SeedAttribute("smtp", "port", "Porta", FieldType.Number, required: true, order: 2, group: "Servidor", description: "Porta TCP. Comuns: 587 (STARTTLS), 465 (SSL), 25.", placeholder: "587");
            SeedAttribute("smtp", "username", "Usuario", FieldType.Text, required: true, order: 3, group: "Autenticação", description: "Usuario de autenticacao SMTP.", placeholder: "usuario@example.com");
            SeedAttribute("smtp", "password", "Senha / App Password", FieldType.Text, required: true, order: 4, group: "Autenticação", sensitive: true, description: "Senha SMTP ou App Password (Gmail/Outlook).");
            SeedAttribute("smtp", "from_email", "Email remetente", FieldType.Text, required: true, order: 5, group: "Remetente", description: "Endereco usado no campo From.", placeholder: "no-reply@empresa.com");
            SeedAttribute("smtp", "from_name", "Nome remetente", FieldType.Text, required: false, order: 6, group: "Remetente", description: "Nome exibido no campo From.", placeholder: "Kanvas");
            SeedAttribute("smtp", "enable_ssl", "Habilitar SSL/TLS", FieldType.Boolean, required: true, order: 7, group: "Servidor", description: "true/false. Use true em portas 587/465.", placeholder: "true");

            BindContract("smtp", "email.send");

            SeedPipeline("smtp", "smtp-send-email", "Enviar email", "Envia e-mail via protocolo SMTP.", isDefault: true, isTestPipeline: false, contractIdentifier: "email.send");
            SeedPipeline("smtp", "smtp-test-connection", "Testar conexao", "Envia e-mail de teste via SMTP para validar credenciais.", isDefault: false, isTestPipeline: true, contractIdentifier: null);

            SeedStep("smtp-send-email", 1, "Enviar e-mail via SMTP", PipelineStepType.SmtpSend, ErrorAction.Stop);
            SeedStep("smtp-test-connection", 1, "Enviar e-mail de teste via SMTP", PipelineStepType.SmtpSend, ErrorAction.Stop);
        }
    }
}
