using FluentMigrator;
using IntegrationPlatform.Domain.ValueObjects;
using IntegrationPlatform.Infrastructure.Migrations.Integrations;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations.ContasAReceber.Asaas
{
    // Integracao Asaas de cobranca (API v3, autenticacao por API key no header access_token, sem mTLS).
    // Decisoes de desenho:
    // - billingType sempre BOLETO: no Asaas o boleto e hibrido (inclui QR PIX), cobrindo method boleto e pix
    //   com um unico fluxo e evitando step condicional (pipeline e linear).
    // - O Asaas exige um customer proprio antes da cobranca; o pipeline cria o cliente a cada emissao
    //   (POST /customers) porque nao ha branch condicional para reusar existente. A correlacao de
    //   conciliacao usa externalReference = financialEntryId, nao o customer.
    // - Liquidacao por PERIODO (reusa cobranca.consultar-liquidados): GET /payments com paymentDate[ge/le]
    //   e status=RECEIVED.
    // - GETs com body vazio (a API retorna 403 se GET tiver body).
    // Depende do catalogo + contratos cobranca.* do SeedSicredi (versao menor).
    [Migration(202607190002)]
    public sealed class Migration_202607190002_SeedAsaas : IntegrationSeedMigration
    {
        public override void Up()
        {
            SeedCategory("contas-a-receber", "Contas a Receber", "Provedores de cobrança e recebíveis (boleto, PIX): registro, baixa e conciliação de liquidados.");

            SeedIntegration("asaas", "Asaas", "Cobrança via Asaas (boleto híbrido com PIX) pela API v3: emissão, cancelamento e conciliação de recebidos, com autenticação por API key.", "contas-a-receber", "https://logos.hunter.io/asaas.com", supportsWebhook: true);

            SeedAttribute("asaas", "api_key", "Chave de API", FieldType.Text, required: true, order: 1, group: "Autenticação", sensitive: true, description: "Chave de API do Asaas (header access_token). Produção: $aact_prod_... | Sandbox: $aact_hmlg_...");
            SeedAttribute("asaas", "base_url", "Base da API", FieldType.Text, required: true, order: 2, group: "Endpoints", hidden: true, description: "Base da API v3. Produção por padrão; sandbox = https://api-sandbox.asaas.com/v3.", defaultValue: "https://api.asaas.com/v3");
            SeedAttribute("asaas", "callbackBaseUrl", "URL base do Mainstay (callback)", FieldType.Text, required: false, order: 3, group: "Webhook", hidden: true, description: "Base do AgencyCampaign que recebe os callbacks.", defaultValue: "https://agencias.mainstay.com.br");

            BindContract("asaas", "cobranca.criar");
            BindContract("asaas", "cobranca.cancelar");
            BindContract("asaas", "cobranca.consultar-liquidados");

            // --- JavaScript functions ---
            SeedJsFunction("asaas-normalizar-payload",
                """
                var doc=(payload.payerDocument==null?'':(''+payload.payerDocument)).replace(/\D/g,'');
                var due=(payload.dueAt==null?'':(''+payload.dueAt)).substring(0,10);
                var cep=(payload.payerCep==null?'':(''+payload.payerCep)).replace(/\D/g,'');
                var juros=Number(payload.interestMonthlyPercent)||0;
                var jurosFrag=juros>0?(',"interest":{"value":'+juros+'}'):'';
                var customerBody={
                  name:(payload.payerName||''),
                  cpfCnpj:doc,
                  postalCode:cep,
                  address:(payload.payerStreet||''),
                  addressNumber:(payload.payerNumber?(''+payload.payerNumber):''),
                  externalReference:(''+(payload.financialEntryId==null?'':payload.financialEntryId))
                };
                result.value={asaasCustomerBody:JSON.stringify(customerBody),dueDate:due,jurosFrag:jurosFrag};
                """,
                "Monta o body do POST /customers Asaas e deriva dueDate (yyyy-MM-dd) e fragmento condicional de juros (interest.value = % ao mês).");

            SeedJsFunction("asaas-normalizar-resposta",
                """
                function s(v){return v==null?'':(''+v);}
                result.value={
                  chargeId:s(variables.id),
                  digitableLine:s(variables.identificationField),
                  barCode:s(variables.barCode),
                  nossoNumeroAsaas:s(variables.nossoNumero),
                  pixCopyPaste:s(variables.payload),
                  txIdAsaas:''
                };
                """,
                "Consolida a emissão Asaas: chargeId (id da cobrança), digitableLine/barCode/nossoNumero (identificationField) e pixCopyPaste (payload do pixQrCode). Asaas não expõe txid.");

            SeedJsFunction("asaas-preparar-consulta",
                """
                var p=(''+(payload.dia||'')).split('/');
                result.value={diaISO:(p.length===3?(p[2]+'-'+p[1]+'-'+p[0]):(''+(payload.dia||'')))};
                """,
                "Converte o dia (dd/MM/yyyy do AgencyCampaign) para yyyy-MM-dd (formato Asaas).");

            SeedJsFunction("asaas-normalizar-liquidados",
                """
                function el(v){if(v==null){return null;}if(typeof v.length!=='number'&&typeof v.ToString==='function'){try{return JSON.parse(v.ToString());}catch(e){return v;}}return v;}
                var items=el(variables.data)||[];
                var out=[];
                for(var i=0;i<items.length;i++){
                  var it=items[i];
                  var fid=(''+(it.externalReference||''));
                  if(fid){
                    out.push({financialEntryId:fid,providerChargeId:(''+(it.id||'')),paidAt:(it.clientPaymentDate||it.paymentDate||null),amountPaid:(Number(it.value)||0)});
                  }
                }
                result.value={liquidados:out};
                """,
                "Normaliza a lista de cobranças RECEIVED do Asaas para { liquidados:[...] }: financialEntryId=externalReference, providerChargeId=id.");

            // --- ApiCalls ---
            SeedApiCall("Asaas - Criar cliente", HttpMethodType.Post, "{{base_url}}/customers",
                """{"access_token":"{{api_key}}","User-Agent":"Mainstay","Content-Type":"application/json"}""",
                "{{asaasCustomerBody}}");

            // "customer":"{{id}}" usa o id retornado pelo POST /customers do step anterior; a resposta
            // deste POST sobrescreve {{id}} com o id da cobranca, consumido pelos GETs seguintes.
            SeedApiCall("Asaas - Criar cobrança", HttpMethodType.Post, "{{base_url}}/payments",
                """{"access_token":"{{api_key}}","User-Agent":"Mainstay","Content-Type":"application/json"}""",
                """{"customer":"{{id}}","billingType":"BOLETO","value":{{amount}},"dueDate":"{{dueDate}}","externalReference":"{{financialEntryId}}"{{jurosFrag}}}""");

            SeedApiCall("Asaas - Obter linha digitável", HttpMethodType.Get, "{{base_url}}/payments/{{id}}/identificationField",
                """{"access_token":"{{api_key}}","User-Agent":"Mainstay"}""",
                "");

            SeedApiCall("Asaas - Obter QR PIX", HttpMethodType.Get, "{{base_url}}/payments/{{id}}/pixQrCode",
                """{"access_token":"{{api_key}}","User-Agent":"Mainstay"}""",
                "");

            SeedApiCall("Asaas - Cancelar cobrança", HttpMethodType.Delete, "{{base_url}}/payments/{{chargeId}}",
                """{"access_token":"{{api_key}}","User-Agent":"Mainstay"}""",
                "");

            SeedApiCall("Asaas - Consultar liquidados", HttpMethodType.Get, "{{base_url}}/payments?paymentDate[ge]={{diaISO}}&paymentDate[le]={{diaISO}}&status=RECEIVED&limit=100",
                """{"access_token":"{{api_key}}","User-Agent":"Mainstay"}""",
                "");

            SeedApiCall("Asaas - Callback issued", HttpMethodType.Post, "{{callbackBaseUrl}}/api/financialentries/provider-callback/{{callbackToken}}",
                """{"x-webhook-secret":"{{CallbackSecret}}","Content-Type":"application/json"}""",
                """{"provider":"asaas","eventType":"issued","financialEntryId":{{financialEntryId}},"chargeId":{{chargeId | json}},"digitableLine":{{digitableLine | json}},"barCode":{{barCode | json}},"nossoNumero":{{nossoNumeroAsaas | json}},"pixCopyPaste":{{pixCopyPaste | json}},"txId":{{txIdAsaas | json}}}""");

            SeedApiCall("Asaas - Callback cancelled", HttpMethodType.Post, "{{callbackBaseUrl}}/api/financialentries/provider-callback/{{callbackToken}}",
                """{"x-webhook-secret":"{{CallbackSecret}}","Content-Type":"application/json"}""",
                """{"provider":"asaas","eventType":"cancelled","chargeId":{{chargeId | json}},"financialEntryId":{{financialEntryId}}}""");

            SeedApiCall("Asaas - Callback falha", HttpMethodType.Post, "{{callbackBaseUrl}}/api/financialentries/provider-callback/{{callbackToken}}",
                """{"x-webhook-secret":"{{CallbackSecret}}","Content-Type":"application/json"}""",
                """{"provider":"asaas","eventType":"failed","financialEntryId":{{financialEntryId}},"metadata":{{errorMessage | json}}}""");

            // --- Pipelines + Steps ---
            SeedPipeline("asaas", "asaas-criar-cobranca", "Criar cobrança", "Cria o cliente e a cobrança no Asaas (boleto híbrido com PIX), busca linha digitável e QR e dispara callback issued.", isDefault: true, isTestPipeline: false, contractIdentifier: "cobranca.criar");
            SeedStep("asaas-criar-cobranca", 1, "Normalizar payload", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "asaas-normalizar-payload");
            SeedStep("asaas-criar-cobranca", 2, "Criar cliente", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Asaas - Criar cliente");
            SeedStep("asaas-criar-cobranca", 3, "Criar cobrança", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Asaas - Criar cobrança");
            SeedStep("asaas-criar-cobranca", 4, "Obter linha digitável", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Asaas - Obter linha digitável");
            SeedStep("asaas-criar-cobranca", 5, "Obter QR PIX", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Asaas - Obter QR PIX");
            SeedStep("asaas-criar-cobranca", 6, "Normalizar resposta", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "asaas-normalizar-resposta");
            SeedStep("asaas-criar-cobranca", 7, "Callback issued", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Asaas - Callback issued", ignoreOnResponse: true);
            SeedStep("asaas-criar-cobranca", 8, "Callback falha", PipelineStepType.HttpRequest, ErrorAction.Continue, apiCall: "Asaas - Callback falha", ignoreOnResponse: true);

            SeedPipeline("asaas", "asaas-cancelar-cobranca", "Cancelar cobrança", "Remove a cobrança no Asaas e dispara callback cancelled.", isDefault: true, isTestPipeline: false, contractIdentifier: "cobranca.cancelar");
            SeedStep("asaas-cancelar-cobranca", 1, "Cancelar cobrança", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Asaas - Cancelar cobrança");
            SeedStep("asaas-cancelar-cobranca", 2, "Callback cancelled", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Asaas - Callback cancelled", ignoreOnResponse: true);
            SeedStep("asaas-cancelar-cobranca", 3, "Callback falha", PipelineStepType.HttpRequest, ErrorAction.Continue, apiCall: "Asaas - Callback falha", ignoreOnResponse: true);

            SeedPipeline("asaas", "asaas-consultar-liquidados", "Consultar liquidados", "Lista as cobranças RECEIVED do dia (modo período) e normaliza para conciliação.", isDefault: true, isTestPipeline: false, contractIdentifier: "cobranca.consultar-liquidados");
            SeedStep("asaas-consultar-liquidados", 1, "Preparar consulta", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "asaas-preparar-consulta");
            SeedStep("asaas-consultar-liquidados", 2, "Consultar liquidados", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Asaas - Consultar liquidados");
            SeedStep("asaas-consultar-liquidados", 3, "Normalizar", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "asaas-normalizar-liquidados");

            // runOnError nos callbacks de falha (o helper base SeedStep nao seta a coluna runonerror).
            Execute.Sql("""
                UPDATE pipelinestep SET runonerror = true, updatedat = now()
                WHERE apicallid = (SELECT id FROM apicall WHERE name = 'Asaas - Callback falha')
                  AND pipelineid IN (SELECT id FROM pipeline WHERE integrationid = (SELECT id FROM integration WHERE identifier = 'asaas'));
                """);
        }

        public override void Down()
        {
            // Catalogo/integracao de convergencia; Down nao reverte (connectors e execucoes referenciam estes registros).
        }
    }
}
