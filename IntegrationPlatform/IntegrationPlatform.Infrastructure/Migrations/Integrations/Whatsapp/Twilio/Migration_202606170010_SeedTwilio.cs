using FluentMigrator;
using IntegrationPlatform.Domain.ValueObjects;
using IntegrationPlatform.Infrastructure.Migrations.Integrations;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations.Whatsapp.Twilio
{
    // Integracao Whatsapp: Twilio WhatsApp (BSP oficial Meta). Idempotente e convergente.
    [Migration(202606170010)]
    public sealed class Migration_202606170010_SeedTwilio : IntegrationSeedMigration
    {
        public override void Up()
        {
            SeedIntegration("twilio-whatsapp", "Twilio WhatsApp", "Envio de WhatsApp via Twilio (BSP oficial Meta).", "whatsapp", "https://logos.hunter.io/twilio.com", supportsWebhook: false);

            SeedAttribute("twilio-whatsapp", "account_sid", "Account SID", FieldType.Text, required: true, order: 1, group: "Autenticação", description: "SID da conta Twilio.", placeholder: "AC...");
            SeedAttribute("twilio-whatsapp", "auth_token", "Auth Token", FieldType.Text, required: true, order: 2, group: "Autenticação", sensitive: true, description: "Auth token correspondente.");
            SeedAttribute("twilio-whatsapp", "from_number", "Numero remetente", FieldType.Text, required: true, order: 3, group: "Remetente", description: "Numero WhatsApp (formato whatsapp:+5511...).", placeholder: "whatsapp:+5511999999999");
            SeedAttribute("twilio-whatsapp", "base_url", "URL base da API", FieldType.Text, required: true, order: 4, group: "Endpoints", hidden: true, description: "Endpoint padrao: https://api.twilio.com/2010-04-01.", placeholder: "https://api.twilio.com/2010-04-01", defaultValue: "https://api.twilio.com/2010-04-01");

            BindContract("twilio-whatsapp", "whatsapp.enviar");

            SeedApiCall("Twilio: Send WhatsApp Message", HttpMethodType.Post, "{{ twilio_url }}",
                """{"Authorization": "Basic {{ twilio_auth }}", "Content-Type": "application/x-www-form-urlencoded"}""",
                "{{ twilio_body }}");

            SeedApiCall("Twilio: Send WhatsApp Template", HttpMethodType.Post, "{{ twilio_url }}",
                """{"Authorization": "Basic {{ twilio_auth }}", "Content-Type": "application/x-www-form-urlencoded"}""",
                "{{ twilio_body }}");

            SeedJsFunction("twilio-montar-envio",
                """
                var accountSid = attributes['account_sid'] || '';
                var authToken  = attributes['auth_token']  || '';
                var fromNumber = attributes['from_number'] || '';
                var baseUrl    = attributes['base_url']    || 'https://api.twilio.com';
                var toNumber   = payload['number']  || '';
                var msgBody    = payload['message'] || '';

                var auth = btoa(accountSid + ':' + authToken);
                variables['twilio_auth'] = auth;
                variables['twilio_url']  = baseUrl + '/2010-04-01/Accounts/' + accountSid + '/Messages.json';
                variables['twilio_body'] = JSON.stringify({
                  From: 'whatsapp:+' + fromNumber,
                  To:   'whatsapp:+' + toNumber,
                  Body: msgBody
                });
                result.value = { twilio_auth: auth };
                """,
                description: "Builds Basic Auth header and form body for Twilio WhatsApp message.");

            SeedJsFunction("twilio-montar-template",
                """
                var accountSid  = attributes['account_sid']  || '';
                var authToken   = attributes['auth_token']   || '';
                var fromNumber  = attributes['from_number']  || '';
                var baseUrl     = attributes['base_url']     || 'https://api.twilio.com';
                var toNumber    = payload['number']          || '';
                var contentSid  = payload['template_name']   || '';
                var contentVars = payload['template_variables'] || '{}';

                var auth = btoa(accountSid + ':' + authToken);
                variables['twilio_auth'] = auth;
                variables['twilio_url']  = baseUrl + '/2010-04-01/Accounts/' + accountSid + '/Messages.json';
                variables['twilio_body'] = JSON.stringify({
                  From:             'whatsapp:+' + fromNumber,
                  To:               'whatsapp:+' + toNumber,
                  ContentSid:       contentSid,
                  ContentVariables: contentVars
                });
                result.value = { twilio_auth: auth };
                """,
                description: "Builds Basic Auth header and form body for Twilio WhatsApp template message.");

            SeedJsFunction("twilio-montar-teste",
                """
                var accountSid = attributes['account_sid'] || '';
                var authToken  = attributes['auth_token']  || '';
                var fromNumber = attributes['from_number'] || '';
                var baseUrl    = attributes['base_url']    || 'https://api.twilio.com';
                var toNumber   = payload['to'] || '';

                var auth = btoa(accountSid + ':' + authToken);
                variables['twilio_auth'] = auth;
                variables['twilio_url']  = baseUrl + '/2010-04-01/Accounts/' + accountSid + '/Messages.json';
                variables['twilio_body'] = JSON.stringify({
                  From: 'whatsapp:+' + fromNumber,
                  To:   'whatsapp:+' + toNumber,
                  Body: 'Teste de integração Mainstay via Twilio WhatsApp.'
                });
                result.value = { twilio_auth: auth };
                """,
                description: "Monta Basic Auth e body para envio de mensagem de teste Twilio WhatsApp, usando payload[to] do modal de teste.");

            SeedPipeline("twilio-whatsapp", "twilio-whatsapp-enviar-mensagem", "Enviar mensagem", "POST Messages.json com From/To/Body.", isDefault: true, isTestPipeline: false, contractIdentifier: "whatsapp.enviar");
            SeedPipeline("twilio-whatsapp", "twilio-whatsapp-enviar-template", "Enviar template aprovado", "Envia template via Content API + Messages.", isDefault: false, isTestPipeline: false, contractIdentifier: null);
            SeedPipeline("twilio-whatsapp", "twilio-whatsapp-testar-conexao", "Testar conexao", "Pipeline de validacao de credenciais.", isDefault: false, isTestPipeline: true, contractIdentifier: null);

            SeedStep("twilio-whatsapp-enviar-mensagem", 1, "Montar autenticacao e corpo", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "twilio-montar-envio");
            SeedStep("twilio-whatsapp-enviar-mensagem", 2, "Enviar mensagem via Twilio", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Twilio: Send WhatsApp Message");
            SeedStep("twilio-whatsapp-enviar-template", 1, "Montar autenticacao e corpo do template", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "twilio-montar-template");
            SeedStep("twilio-whatsapp-enviar-template", 2, "Enviar template via Twilio", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Twilio: Send WhatsApp Template");
            SeedStep("twilio-whatsapp-testar-conexao", 1, "Montar autenticação e corpo da mensagem", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "twilio-montar-teste");
            SeedStep("twilio-whatsapp-testar-conexao", 2, "Enviar mensagem de teste", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Twilio: Send WhatsApp Message");
        }

        public override void Down()
        {
        }
    }
}
