using FluentMigrator;
using IntegrationPlatform.Domain.ValueObjects;
using IntegrationPlatform.Infrastructure.Migrations.Integrations;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations.ContasAReceber.Asaas
{
    // Registro automatico do webhook de pagamento na Asaas (POST/PUT /v3/webhooks), disparado pelo
    // proprio IntegrationPlatform quando o conector e salvo (ver ConnectorsController.RegisterWebhook) -
    // nao depende do operador colar a URL manualmente no painel da Asaas, diferente de ClickSign/ZapSign.
    // Decisoes de desenho:
    // - Idempotente por natureza (nao por chave unica): lista os webhooks da conta, procura um com a URL
    //   alvo (Listar -> Normalizar) e so entao decide Criar (POST, sem match) ou Atualizar (PUT, com
    //   match) via runcondition - assim reenviar a mesma chave de API varias vezes nao duplica webhook
    //   (limite de 10 por conta na Asaas) nem perde o authToken ja em uso.
    // - authToken e gerado por NOS (ConnectorService.RegisterWebhook, RandomNumberGenerator) e persistido
    //   como atributo do conector ANTES do pipeline rodar - nunca deixamos a Asaas auto-gerar, porque
    //   senao nao teriamos como validar o header asaas-access-token nas notificacoes recebidas depois.
    // - asaas-webhook (pipeline receptor, nome pela convencao {integration}-webhook do WebhooksController)
    //   valida esse mesmo authToken contra o header asaas-access-token (payload.webhookHeaders,
    //   preenchido pelo WebhooksController/ExecutionEngineService) antes de repassar o callback -
    //   sem isso qualquer um que descobrisse a URL publica podia forjar "pagamento recebido".
    [Migration(202608250002)]
    public sealed class Migration_202608250002_AsaasRegistrarWebhook : IntegrationSeedMigration
    {
        public override void Up()
        {
            SeedAttribute("asaas", "webhook_auth_token", "Token de validação do webhook", FieldType.Text, required: false, order: 4, group: "Webhook", hidden: true, sensitive: true, description: "Gerado automaticamente e enviado à Asaas como authToken; usado para validar o header asaas-access-token nas notificações recebidas.");

            // --- JavaScript functions ---
            SeedJsFunction("asaas-preparar-registro-webhook",
                """
                result.value={webhookTargetUrl:'https://integrations.mainstay.com.br/api/webhooks/'+(variables.tenantId||'')+'/asaas'};
                """,
                "Monta a URL do nosso receptor de webhook (path {tenantId}/asaas) usada para achar/criar/atualizar o webhook na Asaas.");

            SeedJsFunction("asaas-normalizar-webhooks-existentes",
                """
                function el(v){if(v==null){return [];}if(typeof v.length!=='number'&&typeof v.ToString==='function'){try{return JSON.parse(v.ToString());}catch(e){return [];}}return v;}
                var items=el(variables.data);
                var target=''+(variables.webhookTargetUrl||'');
                var found=null;
                for(var i=0;i<items.length;i++){if((''+(items[i].url||''))===target){found=items[i];break;}}
                result.value={existingWebhookId:found?(''+found.id):'',hasExistingWebhook:!!found};
                """,
                "Procura, na lista de webhooks da conta Asaas, um com a mesma URL alvo (webhookTargetUrl). Define existingWebhookId/hasExistingWebhook para decidir Criar vs Atualizar.");

            SeedJsFunction("asaas-normalizar-webhook-pagamento",
                """
                function s(v){return v==null?'':(''+v);}
                function ob(v){if(v==null){return null;}if(typeof v.ToString==='function'){try{return JSON.parse(v.ToString());}catch(e){return null;}}return v;}
                var headers=ob(payload.webhookHeaders)||{};
                var receivedToken=s(headers['asaas-access-token']);
                if(!secretEquals('webhook_auth_token',receivedToken)){throw new Error('Token de webhook Asaas invalido ou ausente.');}
                var evt=s(payload.event);
                var pay=ob(payload.payment)||{};
                var eventType='';
                if(evt==='PAYMENT_RECEIVED'||evt==='PAYMENT_CONFIRMED'){eventType='paid';}
                else if(evt==='PAYMENT_DELETED'){eventType='cancelled';}
                else {eventType='updated';}
                var financialEntryId=s(pay.externalReference);
                result.value={
                  acEventType:eventType,
                  chargeId:s(pay.id),
                  financialEntryId:financialEntryId,
                  callbackToken:s(variables.tenantId)+'~'+financialEntryId,
                  amountPaid:s(pay.value),
                  paidAt:(pay.clientPaymentDate||pay.paymentDate||null)
                };
                """,
                "Valida o header asaas-access-token contra o webhook_auth_token do conector (lanca erro se nao bater, interrompendo o pipeline antes do callback) e traduz o evento Asaas (PAYMENT_RECEIVED/PAYMENT_CONFIRMED -> paid, PAYMENT_DELETED -> cancelled) para o callback do Mainstay.");

            // --- ApiCalls ---
            SeedApiCall("Asaas - Listar webhooks", HttpMethodType.Get, "{{base_url}}/webhooks",
                """{"access_token":"{{api_key}}","User-Agent":"Mainstay"}""",
                "");

            string webhookBody = """{"name":"Mainstay","url":"{{webhookTargetUrl}}","email":"integracoes@mainstay.com.br","enabled":true,"interrupted":false,"apiVersion":3,"authToken":"{{webhook_auth_token}}","sendType":"SEQUENTIALLY","events":["PAYMENT_RECEIVED","PAYMENT_CONFIRMED","PAYMENT_DELETED"]}""";

            SeedApiCall("Asaas - Criar webhook", HttpMethodType.Post, "{{base_url}}/webhooks",
                """{"access_token":"{{api_key}}","User-Agent":"Mainstay","Content-Type":"application/json"}""",
                webhookBody);

            SeedApiCall("Asaas - Atualizar webhook", HttpMethodType.Put, "{{base_url}}/webhooks/{{existingWebhookId}}",
                """{"access_token":"{{api_key}}","User-Agent":"Mainstay","Content-Type":"application/json"}""",
                webhookBody);

            SeedApiCall("Asaas - Callback webhook pagamento", HttpMethodType.Post, "{{callbackBaseUrl}}/api/financialentries/provider-callback/{{callbackToken}}",
                """{"x-webhook-secret":"{{CallbackSecret}}","Content-Type":"application/json"}""",
                """{"provider":"asaas","eventType":{{acEventType | json}},"financialEntryId":{{financialEntryId}},"chargeId":{{chargeId | json}},"amountPaid":{{amountPaid}},"paidAt":{{paidAt | json}}}""");

            // --- Pipeline: Registrar webhook (disparado por ConnectorsController.RegisterWebhook ao salvar) ---
            SeedPipeline("asaas", "asaas-registrar-webhook", "Registrar webhook", "Garante (idempotente) que a Asaas tem um webhook cadastrado apontando para o receptor do Mainstay, criando ou atualizando conforme necessario.", isDefault: false, isTestPipeline: false, contractIdentifier: null);
            SeedStep("asaas-registrar-webhook", 1, "Preparar registro", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "asaas-preparar-registro-webhook");
            SeedStep("asaas-registrar-webhook", 2, "Listar webhooks", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Asaas - Listar webhooks");
            SeedStep("asaas-registrar-webhook", 3, "Normalizar webhooks existentes", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "asaas-normalizar-webhooks-existentes");
            SeedStep("asaas-registrar-webhook", 4, "Criar webhook", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Asaas - Criar webhook");
            SeedStep("asaas-registrar-webhook", 5, "Atualizar webhook", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Asaas - Atualizar webhook");

            Execute.Sql("""
                UPDATE pipelinestep SET runcondition = '!variables.hasExistingWebhook', updatedat = now()
                WHERE pipelineid = (SELECT id FROM pipeline WHERE identifier = 'asaas-registrar-webhook') AND "order" = 4;
                """);
            Execute.Sql("""
                UPDATE pipelinestep SET runcondition = '!!variables.hasExistingWebhook', updatedat = now()
                WHERE pipelineid = (SELECT id FROM pipeline WHERE identifier = 'asaas-registrar-webhook') AND "order" = 5;
                """);

            // --- Pipeline: Webhook (receptor de PAYMENT_RECEIVED/PAYMENT_CONFIRMED/PAYMENT_DELETED) ---
            SeedPipeline("asaas", "asaas-webhook", "Webhook Asaas", "Recebe as notificacoes de pagamento da Asaas, valida o authToken e repassa ao callback do Mainstay.", isDefault: false, isTestPipeline: false, contractIdentifier: null);
            SeedStep("asaas-webhook", 1, "Normalizar webhook", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "asaas-normalizar-webhook-pagamento");
            SeedStep("asaas-webhook", 2, "Callback webhook", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Asaas - Callback webhook pagamento", ignoreOnResponse: true);
        }

        public override void Down()
        {
            // Catalogo/integracao de convergencia; Down nao reverte (connectors e execucoes referenciam estes registros).
        }
    }
}
