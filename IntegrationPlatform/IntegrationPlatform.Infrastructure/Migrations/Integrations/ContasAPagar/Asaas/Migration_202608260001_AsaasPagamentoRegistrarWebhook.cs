using FluentMigrator;
using IntegrationPlatform.Domain.ValueObjects;
using IntegrationPlatform.Infrastructure.Migrations.Integrations;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations.ContasAPagar.Asaas
{
    // Registro automatico do webhook de TRANSFERENCIA na Asaas (POST/PUT /v3/webhooks), mesmo padrao ja
    // usado para o webhook de pagamento (cobranca) em ContasAReceber/Asaas - reaproveita o endpoint generico
    // ConnectorsController.RegisterWebhook (que so procura "{identifier}-registrar-webhook" por convencao,
    // sem nenhum conhecimento de Asaas), entao nao exige mudanca nenhuma fora desta migration.
    //
    // Por que isso e necessario: o pagamento.pix ja e cuidadoso (so registra "accepted" apos o POST
    // /transfers, nunca marca pago sem confirmacao - ver HandleProviderCallback no AgencyCampaign), mas
    // sem webhook NADA avanca o repasse de Agendado para Pago depois que a Asaas confirma a transferencia
    // (a Asaas exige autorizacao manual de saque no painel deles, por seguranca contra chave vazada).
    // Confirmado ao vivo (2026-08-25): repasse de R$3.000 pra Bia Salgado ficou "Agendado" indefinidamente,
    // e o painel da Asaas mostrava "1 evento critico pendente" pedindo autorizacao manual do saque.
    //
    // Eventos de transferencia (developers.asaas.com/docs/webhook-para-transferencias):
    // TRANSFER_CREATED, TRANSFER_PENDING, TRANSFER_IN_BANK_PROCESSING, TRANSFER_BLOCKED, TRANSFER_DONE,
    // TRANSFER_FAILED, TRANSFER_CANCELLED. So assinamos os 3 eventos terminais.
    [Migration(202608260001)]
    public sealed class Migration_202608260001_AsaasPagamentoRegistrarWebhook : IntegrationSeedMigration
    {
        public override void Up()
        {
            SeedAttribute("asaas-pagamento", "webhook_auth_token", "Token de validação do webhook", FieldType.Text, required: false, order: 4, group: "Webhook", hidden: true, sensitive: true, description: "Gerado automaticamente e enviado à Asaas como authToken; usado para validar o header asaas-access-token nas notificações recebidas.");

            // --- JavaScript functions ---
            SeedJsFunction("asaas-pagamento-preparar-registro-webhook",
                """
                result.value={webhookTargetUrl:'https://integrations.mainstay.com.br/api/webhooks/'+(variables.tenantId||'')+'/asaas-pagamento'};
                """,
                "Monta a URL do nosso receptor de webhook (path {tenantId}/asaas-pagamento) usada para achar/criar/atualizar o webhook na Asaas.");

            SeedJsFunction("asaas-pagamento-normalizar-webhooks-existentes",
                """
                function el(v){if(v==null){return [];}if(typeof v.length!=='number'&&typeof v.ToString==='function'){try{return JSON.parse(v.ToString());}catch(e){return [];}}return v;}
                var items=el(variables.data);
                var target=''+(variables.webhookTargetUrl||'');
                var found=null;
                for(var i=0;i<items.length;i++){if((''+(items[i].url||''))===target){found=items[i];break;}}
                result.value={existingWebhookId:found?(''+found.id):'',hasExistingWebhook:!!found};
                """,
                "Procura, na lista de webhooks da conta Asaas, um com a mesma URL alvo (webhookTargetUrl). Define existingWebhookId/hasExistingWebhook para decidir Criar vs Atualizar.");

            SeedJsFunction("asaas-pagamento-normalizar-webhook-transferencia",
                """
                function s(v){return v==null?'':(''+v);}
                function ob(v){if(v==null){return null;}if(typeof v.ToString==='function'){try{return JSON.parse(v.ToString());}catch(e){return null;}}return v;}
                var headers=ob(payload.webhookHeaders)||{};
                var receivedToken=s(headers['asaas-access-token']);
                if(!secretEquals('webhook_auth_token',receivedToken)){throw new Error('Token de webhook Asaas invalido ou ausente.');}
                var evt=s(payload.event);
                var tr=ob(payload.transfer)||{};
                var eventType='';
                if(evt==='TRANSFER_DONE'){eventType='completed';}
                else if(evt==='TRANSFER_FAILED'){eventType='failed';}
                else if(evt==='TRANSFER_CANCELLED'){eventType='cancelled';}
                else {eventType='processing';}
                var transferId=s(tr.id);
                result.value={
                  acEventType:eventType,
                  providerTransactionId:transferId,
                  callbackToken:s(variables.tenantId)+'~'+transferId,
                  endToEndId:s(tr.endToEndIdentifier),
                  failureReason:s(tr.failReason)
                };
                """,
                "Valida o header asaas-access-token contra o webhook_auth_token do conector e traduz o evento de transferencia Asaas (TRANSFER_DONE->completed, TRANSFER_FAILED->failed, TRANSFER_CANCELLED->cancelled) para o callback do Mainstay. callbackToken e montado aqui mesmo (tenantId~transferId) porque {{callbackToken}} nunca resolve num pipeline disparado por webhook.");

            // --- ApiCalls ---
            SeedApiCall("Asaas Pagamento - Listar webhooks", HttpMethodType.Get, "{{base_url}}/webhooks",
                """{"access_token":"{{api_key}}","User-Agent":"Mainstay"}""",
                "");

            string webhookBody = """{"name":"Mainstay Pagamentos","url":"{{webhookTargetUrl}}","email":"integracoes@mainstay.com.br","enabled":true,"interrupted":false,"apiVersion":3,"authToken":"{{webhook_auth_token}}","sendType":"SEQUENTIALLY","events":["TRANSFER_DONE","TRANSFER_FAILED","TRANSFER_CANCELLED"]}""";

            SeedApiCall("Asaas Pagamento - Criar webhook", HttpMethodType.Post, "{{base_url}}/webhooks",
                """{"access_token":"{{api_key}}","User-Agent":"Mainstay","Content-Type":"application/json"}""",
                webhookBody);

            SeedApiCall("Asaas Pagamento - Atualizar webhook", HttpMethodType.Put, "{{base_url}}/webhooks/{{existingWebhookId}}",
                """{"access_token":"{{api_key}}","User-Agent":"Mainstay","Content-Type":"application/json"}""",
                webhookBody);

            SeedApiCall("Asaas Pagamento - Callback webhook transferência", HttpMethodType.Post, "{{callbackBaseUrl}}/api/creatorpayments/provider-callback/{{callbackToken}}",
                """{"x-webhook-secret":"{{CallbackSecret}}","Content-Type":"application/json"}""",
                """{"provider":"asaas","eventType":{{acEventType | json}},"providerTransactionId":{{providerTransactionId | json}},"endToEndId":{{endToEndId | json}},"failureReason":{{failureReason | json}},"metadata":{{failureReason | json}}}""");

            // --- Pipeline: Registrar webhook (disparado por ConnectorsController.RegisterWebhook ao salvar) ---
            SeedPipeline("asaas-pagamento", "asaas-pagamento-registrar-webhook", "Registrar webhook", "Garante (idempotente) que a Asaas tem um webhook cadastrado apontando para o receptor do Mainstay, criando ou atualizando conforme necessario.", isDefault: false, isTestPipeline: false, contractIdentifier: null);
            SeedStep("asaas-pagamento-registrar-webhook", 1, "Preparar registro", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "asaas-pagamento-preparar-registro-webhook");
            SeedStep("asaas-pagamento-registrar-webhook", 2, "Listar webhooks", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Asaas Pagamento - Listar webhooks");
            SeedStep("asaas-pagamento-registrar-webhook", 3, "Normalizar webhooks existentes", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "asaas-pagamento-normalizar-webhooks-existentes");
            SeedStep("asaas-pagamento-registrar-webhook", 4, "Criar webhook", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Asaas Pagamento - Criar webhook");
            SeedStep("asaas-pagamento-registrar-webhook", 5, "Atualizar webhook", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Asaas Pagamento - Atualizar webhook");

            Execute.Sql("""
                UPDATE pipelinestep SET runcondition = '!variables.hasExistingWebhook', updatedat = now()
                WHERE pipelineid = (SELECT id FROM pipeline WHERE identifier = 'asaas-pagamento-registrar-webhook') AND "order" = 4;
                """);
            Execute.Sql("""
                UPDATE pipelinestep SET runcondition = '!!variables.hasExistingWebhook', updatedat = now()
                WHERE pipelineid = (SELECT id FROM pipeline WHERE identifier = 'asaas-pagamento-registrar-webhook') AND "order" = 5;
                """);

            // --- Pipeline: Webhook (receptor de TRANSFER_DONE/TRANSFER_FAILED/TRANSFER_CANCELLED) ---
            SeedPipeline("asaas-pagamento", "asaas-pagamento-webhook", "Webhook Asaas Pagamentos", "Recebe as notificacoes de transferencia da Asaas, valida o authToken e repassa ao callback do Mainstay.", isDefault: false, isTestPipeline: false, contractIdentifier: null);
            SeedStep("asaas-pagamento-webhook", 1, "Normalizar webhook", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "asaas-pagamento-normalizar-webhook-transferencia");
            SeedStep("asaas-pagamento-webhook", 2, "Callback webhook", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Asaas Pagamento - Callback webhook transferência", ignoreOnResponse: true);
        }

        public override void Down()
        {
            // Catalogo/integracao de convergencia; Down nao reverte (connectors e execucoes referenciam estes registros).
        }
    }
}
