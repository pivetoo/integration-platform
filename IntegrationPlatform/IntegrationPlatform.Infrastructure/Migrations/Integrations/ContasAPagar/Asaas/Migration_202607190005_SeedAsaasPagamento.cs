using FluentMigrator;
using IntegrationPlatform.Domain.ValueObjects;
using IntegrationPlatform.Infrastructure.Migrations.Integrations;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations.ContasAPagar.Asaas
{
    // Integracao de PAGAMENTO Asaas (Contas a Pagar) — API v3, mesma API key da cobranca, sem mTLS.
    // Tres formas de pagar a partir do saldo da conta Asaas:
    // - PIX por chave (POST /transfers) no contrato pagamento.pix EXISTENTE: payload e callback identicos
    //   ao santander-pagamento, entao o AgencyCampaign (CreatorPaymentService -> intent repasse.agendar-pix,
    //   callback /api/creatorpayments/provider-callback correlacionado por idempotencyKey) funciona sem mudanca.
    // - Boleto por linha digitavel (POST /bill) e PIX QR/copia-e-cola (POST /pix/qrCodes/pay) em contratos
    //   novos (pagamento.boleto / pagamento.qrcode), SEM callback por enquanto: o fluxo de pagamento a
    //   fornecedor ainda nao existe no AgencyCampaign; os pipelines retornam a resposta normalizada.
    // Integracao separada da cobranca (asaas-pagamento) porque integracao pertence a uma categoria.
    [Migration(202607190005)]
    public sealed class Migration_202607190005_SeedAsaasPagamento : IntegrationSeedMigration
    {
        public override void Up()
        {
            SeedCategory("contas-a-pagar", "Contas a Pagar", "Provedores de pagamento (PIX, boleto, concessionaria): repasses e pagamentos de contas.");

            SeedContract(
                identifier: "pagamento.boleto",
                name: "Pagamento de boleto",
                description: "Paga um boleto de terceiro pela linha digitavel, com agendamento opcional. Retorna o pagamento criado no provedor.",
                categoryIdentifier: "contas-a-pagar",
                inputSchema: """{ "payableId": "number?", "idempotencyKey": "string", "callbackToken": "string", "identificationField": "string linha digitavel", "amount": "number?", "dueDate": "string date?", "description": "string?", "scheduleDate": "string date?" }""",
                outputSchema: """{ "providerPaymentId": "string", "statusAsaas": "string", "feeAsaas": "number" }""",
                hasCallback: false,
                callbackSchema: null);

            SeedContract(
                identifier: "pagamento.qrcode",
                name: "Pagamento de QR Code PIX",
                description: "Paga um QR Code / copia-e-cola PIX, com agendamento opcional. Retorna o pagamento criado no provedor.",
                categoryIdentifier: "contas-a-pagar",
                inputSchema: """{ "payableId": "number?", "idempotencyKey": "string", "callbackToken": "string", "pixPayload": "string copia-e-cola", "amount": "number?", "description": "string?", "scheduleDate": "string date?" }""",
                outputSchema: """{ "providerPaymentId": "string", "statusAsaas": "string", "endToEndId": "string?" }""",
                hasCallback: false,
                callbackSchema: null);

            // --- Integracao asaas-pagamento ---
            SeedIntegration("asaas-pagamento", "Asaas", "Pagamentos Asaas (PIX por chave, boleto e QR Code) pela API v3, a partir do saldo da conta Asaas, com autenticação por API key.", "contas-a-pagar", "https://logos.hunter.io/asaas.com", supportsWebhook: true);

            SeedAttribute("asaas-pagamento", "api_key", "Chave de API", FieldType.Text, required: true, order: 1, group: "Autenticação", sensitive: true, description: "Chave de API do Asaas (header access_token), a mesma da cobrança. Produção: $aact_prod_... | Sandbox: $aact_hmlg_...");
            SeedAttribute("asaas-pagamento", "base_url", "Base da API", FieldType.Text, required: true, order: 2, group: "Endpoints", hidden: true, description: "Base da API v3. Produção por padrão; sandbox = https://api-sandbox.asaas.com/v3.", defaultValue: "https://api.asaas.com/v3");
            SeedAttribute("asaas-pagamento", "callbackBaseUrl", "URL base do Mainstay (callback)", FieldType.Text, required: false, order: 3, group: "Webhook", hidden: true, description: "Base do AgencyCampaign que recebe os callbacks.", defaultValue: "https://agencias.mainstay.com.br");

            BindContract("asaas-pagamento", "pagamento.pix");
            BindContract("asaas-pagamento", "pagamento.boleto");
            BindContract("asaas-pagamento", "pagamento.qrcode");

            // --- JavaScript functions ---
            SeedJsFunction("asaas-pagamento-normalizar-pix",
                """
                function kt(t){t=(''+(t==null?'':t)).toUpperCase();if(t==='RANDOM'){return 'EVP';}return t;}
                function clean(s){return (''+(s==null?'':s)).replace(/^\s+|\s+$/g,'');}
                var valor=Number(payload.netAmount)||0;
                var body={value:valor,pixAddressKey:clean(payload.pixKey),pixAddressKeyType:kt(payload.pixKeyType),operationType:'PIX',description:clean(payload.description||'Repasse Mainstay').substring(0,100)};
                result.value={transferBody:JSON.stringify(body)};
                """,
                "Monta o body do POST /transfers Asaas: value=netAmount, pixAddressKey/pixAddressKeyType (Cpf|Cnpj|Email|Phone->maiusculo, Random->EVP), operationType PIX.");

            SeedJsFunction("asaas-pagamento-resposta-pix",
                """
                function s(v){return v==null?'':(''+v);}
                result.value={providerTransactionId:s(variables.id),providerPaymentId:s(variables.id),endToEndId:s(variables.endToEndIdentifier),statusAsaas:s(variables.status)};
                """,
                "Extrai id/status/endToEndIdentifier da transferência Asaas para o callback paid (providerTransactionId=providerPaymentId=id).");

            SeedJsFunction("asaas-pagamento-normalizar-boleto",
                """
                var line=(''+(payload.identificationField==null?'':payload.identificationField)).replace(/[^0-9]/g,'');
                var valor=Number(payload.amount)||0;
                var due=(payload.dueDate==null?'':(''+payload.dueDate)).substring(0,10);
                var sched=(payload.scheduleDate==null?'':(''+payload.scheduleDate)).substring(0,10);
                var body={identificationField:line,description:(''+(payload.description||'')).substring(0,100),externalReference:(''+(payload.idempotencyKey==null?'':payload.idempotencyKey))};
                if(valor>0){body.value=valor;}
                if(due){body.dueDate=due;}
                if(sched){body.scheduleDate=sched;}
                result.value={billBody:JSON.stringify(body)};
                """,
                "Monta o body do POST /bill Asaas: linha digitavel so digitos, externalReference=idempotencyKey, value/dueDate/scheduleDate apenas quando informados.");

            SeedJsFunction("asaas-pagamento-resposta-boleto",
                """
                function s(v){return v==null?'':(''+v);}
                result.value={providerPaymentId:s(variables.id),statusAsaas:s(variables.status),feeAsaas:(Number(variables.fee)||0)};
                """,
                "Extrai id/status/fee do pagamento de boleto Asaas.");

            SeedJsFunction("asaas-pagamento-normalizar-qrcode",
                """
                var valor=Number(payload.amount)||0;
                var sched=(payload.scheduleDate==null?'':(''+payload.scheduleDate)).substring(0,10);
                var body={qrCode:{payload:(''+(payload.pixPayload==null?'':payload.pixPayload)).replace(/^\s+|\s+$/g,'')},description:(''+(payload.description||'')).substring(0,100)};
                if(valor>0){body.value=valor;}
                if(sched){body.scheduleDate=sched;}
                result.value={qrBody:JSON.stringify(body)};
                """,
                "Monta o body do POST /pix/qrCodes/pay Asaas: qrCode.payload=copia-e-cola, value apenas quando informado (QR dinamico ja traz o valor).");

            SeedJsFunction("asaas-pagamento-resposta-qrcode",
                """
                function s(v){return v==null?'':(''+v);}
                result.value={providerPaymentId:s(variables.id),statusAsaas:s(variables.status),endToEndId:s(variables.endToEndIdentifier)};
                """,
                "Extrai id/status/endToEndIdentifier do pagamento de QR Code Asaas.");

            // --- ApiCalls ---
            SeedApiCall("Asaas Pagamento - Transferir PIX", HttpMethodType.Post, "{{base_url}}/transfers",
                """{"access_token":"{{api_key}}","User-Agent":"Mainstay","Content-Type":"application/json"}""",
                "{{transferBody}}");

            SeedApiCall("Asaas Pagamento - Pagar boleto", HttpMethodType.Post, "{{base_url}}/bill",
                """{"access_token":"{{api_key}}","User-Agent":"Mainstay","Content-Type":"application/json"}""",
                "{{billBody}}");

            SeedApiCall("Asaas Pagamento - Pagar QR Code", HttpMethodType.Post, "{{base_url}}/pix/qrCodes/pay",
                """{"access_token":"{{api_key}}","User-Agent":"Mainstay","Content-Type":"application/json"}""",
                "{{qrBody}}");

            SeedApiCall("Asaas Pagamento - Callback pago", HttpMethodType.Post, "{{callbackBaseUrl}}/api/creatorpayments/provider-callback/{{callbackToken}}",
                """{"x-webhook-secret":"{{CallbackSecret}}","Content-Type":"application/json"}""",
                """{"provider":"asaas","eventType":"paid","idempotencyKey":{{idempotencyKey | json}},"providerTransactionId":{{providerTransactionId | json}},"endToEndId":{{endToEndId | json}}}""");

            SeedApiCall("Asaas Pagamento - Callback falha", HttpMethodType.Post, "{{callbackBaseUrl}}/api/creatorpayments/provider-callback/{{callbackToken}}",
                """{"x-webhook-secret":"{{CallbackSecret}}","Content-Type":"application/json"}""",
                """{"provider":"asaas","eventType":"failed","idempotencyKey":{{idempotencyKey | json}},"failureReason":{{errorMessage | json}},"metadata":{{errorMessage | json}}}""");

            // --- Pipelines + Steps ---
            SeedPipeline("asaas-pagamento", "asaas-pagamento-pix", "Pagar PIX", "Efetiva um repasse PIX por chave no Asaas (POST /transfers) e confirma via callback paid/failed.", isDefault: true, isTestPipeline: false, contractIdentifier: "pagamento.pix");
            SeedStep("asaas-pagamento-pix", 1, "Normalizar pagamento", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "asaas-pagamento-normalizar-pix");
            SeedStep("asaas-pagamento-pix", 2, "Transferir PIX", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Asaas Pagamento - Transferir PIX");
            SeedStep("asaas-pagamento-pix", 3, "Normalizar resposta", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "asaas-pagamento-resposta-pix");
            SeedStep("asaas-pagamento-pix", 4, "Callback pago", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Asaas Pagamento - Callback pago", ignoreOnResponse: true);
            SeedStep("asaas-pagamento-pix", 5, "Callback falha", PipelineStepType.HttpRequest, ErrorAction.Continue, apiCall: "Asaas Pagamento - Callback falha", ignoreOnResponse: true);

            SeedPipeline("asaas-pagamento", "asaas-pagamento-boleto", "Pagar boleto", "Paga um boleto pela linha digitável no Asaas (POST /bill) e retorna o pagamento normalizado.", isDefault: true, isTestPipeline: false, contractIdentifier: "pagamento.boleto");
            SeedStep("asaas-pagamento-boleto", 1, "Normalizar pagamento", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "asaas-pagamento-normalizar-boleto");
            SeedStep("asaas-pagamento-boleto", 2, "Pagar boleto", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Asaas Pagamento - Pagar boleto");
            SeedStep("asaas-pagamento-boleto", 3, "Normalizar resposta", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "asaas-pagamento-resposta-boleto");

            SeedPipeline("asaas-pagamento", "asaas-pagamento-qrcode", "Pagar QR Code", "Paga um QR Code / copia-e-cola PIX no Asaas (POST /pix/qrCodes/pay) e retorna o pagamento normalizado.", isDefault: true, isTestPipeline: false, contractIdentifier: "pagamento.qrcode");
            SeedStep("asaas-pagamento-qrcode", 1, "Normalizar pagamento", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "asaas-pagamento-normalizar-qrcode");
            SeedStep("asaas-pagamento-qrcode", 2, "Pagar QR Code", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Asaas Pagamento - Pagar QR Code");
            SeedStep("asaas-pagamento-qrcode", 3, "Normalizar resposta", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "asaas-pagamento-resposta-qrcode");

            // runOnError no callback de falha (o helper base SeedStep nao seta a coluna runonerror).
            Execute.Sql("""
                UPDATE pipelinestep SET runonerror = true, updatedat = now()
                WHERE apicallid = (SELECT id FROM apicall WHERE name = 'Asaas Pagamento - Callback falha')
                  AND pipelineid IN (SELECT id FROM pipeline WHERE integrationid = (SELECT id FROM integration WHERE identifier = 'asaas-pagamento'));
                """);
        }

        public override void Down()
        {
            // Catalogo/integracao de convergencia; Down nao reverte (connectors e execucoes referenciam estes registros).
        }
    }
}
