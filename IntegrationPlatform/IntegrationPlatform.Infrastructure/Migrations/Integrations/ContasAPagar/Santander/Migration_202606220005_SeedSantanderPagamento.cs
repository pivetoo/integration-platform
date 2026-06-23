using FluentMigrator;
using IntegrationPlatform.Domain.ValueObjects;
using IntegrationPlatform.Infrastructure.Migrations.Integrations;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations.ContasAPagar.Santander
{
    // Integracao de PAGAMENTO Santander (Contas a Pagar) — repasse PIX por chave, API management_payments_partners v1, mTLS.
    // Greenfield: cria a categoria contas-a-pagar e o contrato pagamento.pix. Integracao separada da cobranca
    // (identifier santander-pagamento), pois e outro produto de API (workspace e conector proprios).
    // Padrao de 2 passos: POST cria (id) -> PATCH /{id} status AUTHORIZED (transaction.code) -> callback paid/failed.
    // Idempotente e convergente (INSERT WHERE NOT EXISTS por chave natural).
    [Migration(202606220005)]
    public sealed class Migration_202606220005_SeedSantanderPagamento : IntegrationSeedMigration
    {
        public override void Up()
        {
            SeedCategory("contas-a-pagar", "Contas a Pagar", "Provedores de pagamento (PIX, boleto, concessionaria): repasses e pagamentos de contas.");

            SeedContract(
                identifier: "pagamento.pix",
                name: "Pagamento PIX",
                description: "Efetiva um repasse PIX por chave ao destinatario, em 2 passos (cria + autoriza). Confirma via callback paid/failed.",
                categoryIdentifier: "contas-a-pagar",
                inputSchema: """{ "creatorPaymentId": "number", "idempotencyKey": "string", "callbackToken": "string", "netAmount": "number", "pixKey": "string", "pixKeyType": "Cpf|Cnpj|Email|Phone|Random", "creatorName": "string", "creatorDocument": "string", "description": "string?" }""",
                outputSchema: """{ "providerTransactionId": "string", "providerPaymentId": "string", "endToEndId": "string?", "status": "string" }""",
                hasCallback: false,
                callbackSchema: null);

            // --- Integracao santander-pagamento ---
            SeedIntegration("santander-pagamento", "Santander", "Pagamentos Santander (PIX) via API management_payments_partners v1, com mTLS.", "contas-a-pagar", "https://logos.hunter.io/santander.com.br", supportsWebhook: true);

            SeedAttribute("santander-pagamento", "client_id", "Client ID", FieldType.Text, required: true, order: 1, group: "Autenticacao", sensitive: true, description: "Client ID da aplicacao no developer.santander.com.br (tambem usado como X-Application-Key).");
            SeedAttribute("santander-pagamento", "client_secret", "Client Secret", FieldType.Text, required: true, order: 2, group: "Autenticacao", sensitive: true, description: "Client Secret da aplicacao.");
            SeedAttribute("santander-pagamento", "client_cert_pfx", "Certificado (.pfx)", FieldType.File, required: true, order: 3, group: "Certificado", sensitive: true, description: "Certificado A1 (.pfx) - mTLS OBRIGATORIO no Santander.");
            SeedAttribute("santander-pagamento", "client_cert_password", "Senha do certificado", FieldType.Text, required: true, order: 4, group: "Certificado", sensitive: true, description: "Senha do .pfx.");
            SeedAttribute("santander-pagamento", "workspace_id", "Workspace ID", FieldType.Text, required: true, order: 5, group: "Pagador", description: "ID do workspace de pagamentos (management_payments_partners).");
            SeedAttribute("santander-pagamento", "debitBranch", "Agencia de debito", FieldType.Text, required: true, order: 6, group: "Pagador", description: "Agencia da conta de debito do pagador (debitAccount.branch).");
            SeedAttribute("santander-pagamento", "debitAccount", "Conta de debito", FieldType.Text, required: true, order: 7, group: "Pagador", description: "Numero da conta de debito do pagador (debitAccount.number).");
            SeedAttribute("santander-pagamento", "environment", "Ambiente", FieldType.Text, required: true, order: 8, group: "Endpoints", hidden: true, description: "PRODUCAO ou TESTE.", defaultValue: "PRODUCAO");
            SeedAttribute("santander-pagamento", "base_url", "Base da API", FieldType.Text, required: true, order: 9, group: "Endpoints", hidden: true, description: "Base da API trust-open. Producao por padrao.", defaultValue: "https://trust-open.api.santander.com.br");
            SeedAttribute("santander-pagamento", "callbackBaseUrl", "URL base do Mainstay (callback)", FieldType.Text, required: false, order: 10, group: "Webhook", hidden: true, description: "Base do AgencyCampaign que recebe os callbacks.", defaultValue: "https://agencias.mainstay.com.br");

            BindContract("santander-pagamento", "pagamento.pix");

            // --- JavaScript functions ---
            SeedJsFunction("santander-pagamento-normalizar-pix",
                """
                function uuidFrom(hex){var h=(''+(hex==null?'':hex)).replace(/[^0-9a-fA-F]/g,'').toLowerCase();while(h.length<32){h+='0';}h=h.substring(0,32);return h.substring(0,8)+'-'+h.substring(8,12)+'-'+h.substring(12,16)+'-'+h.substring(16,20)+'-'+h.substring(20,32);}
                function kt(t){t=(''+(t==null?'':t)).toUpperCase();if(t==='PHONE'){return 'CELULAR';}if(t==='RANDOM'){return 'EVP';}return t;}
                function clean(s){return (''+(s==null?'':s)).replace(/^\s+|\s+$/g,'');}
                var valor=Number(payload.netAmount)||0;
                var body={id:uuidFrom(payload.idempotencyKey),dictCodeType:kt(payload.pixKeyType),dictCode:clean(payload.pixKey),paymentValue:valor.toFixed(2),remittanceInformation:clean(payload.description||'Repasse Mainstay').substring(0,140),tags:['mainstay','pix']};
                result.value={pixBody:JSON.stringify(body),paymentValue:valor};
                """,
                "Monta o body do POST de PIX (id UUID derivado da idempotencyKey, dictCodeType do pixKeyType, dictCode=pixKey, paymentValue 2 casas, remittanceInformation, tags) e expoe paymentValue numerico para o PATCH.");

            SeedJsFunction("santander-pagamento-extrair-id",
                """
                function el(v){if(v==null){return null;}if(typeof v.length!=='number'&&typeof v.ToString==='function'){try{return JSON.parse(v.ToString());}catch(e){return v;}}return v;}
                var root=el(variables.data)||{};
                result.value={paymentId:(''+(root.id==null?'':root.id))};
                """,
                "Extrai o id do pagamento PIX da resposta do POST (passo 1) para compor a URL do PATCH de autorizacao.");

            SeedJsFunction("santander-pagamento-normalizar-resposta",
                """
                function el(v){if(v==null){return null;}if(typeof v.length!=='number'&&typeof v.ToString==='function'){try{return JSON.parse(v.ToString());}catch(e){return v;}}return v;}
                var root=el(variables.data)||{};
                var tx=root.transaction||{};
                var code=(tx.code!=null?tx.code:(root.id||''));
                var e2e=(root.endToEndId!=null?root.endToEndId:(tx.endToEndId||''));
                result.value={providerTransactionId:(''+(code||'')),providerPaymentId:(''+(variables.paymentId==null?'':variables.paymentId)),endToEndId:(''+(e2e||''))};
                """,
                "Extrai providerTransactionId (transaction.code), providerPaymentId e endToEndId da resposta do PATCH AUTHORIZED para o callback.");

            // --- ApiCalls ---
            SeedApiCall("Santander Pagamento - Autenticacao", HttpMethodType.Post, "{{base_url}}/auth/oauth/v2/token",
                """{"Content-Type":"application/x-www-form-urlencoded"}""",
                "client_id={{client_id}}&client_secret={{client_secret}}&grant_type=client_credentials");

            SeedApiCall("Santander Pagamento - Criar PIX", HttpMethodType.Post, "{{base_url}}/management_payments_partners/v1/workspaces/{{workspace_id}}/pix_payments",
                """{"Authorization":"Bearer {{access_token}}","X-Application-Key":"{{client_id}}","Content-Type":"application/json"}""",
                "{{pixBody}}");

            SeedApiCall("Santander Pagamento - Autorizar PIX", HttpMethodType.Patch, "{{base_url}}/management_payments_partners/v1/workspaces/{{workspace_id}}/pix_payments/{{paymentId}}",
                """{"Authorization":"Bearer {{access_token}}","X-Application-Key":"{{client_id}}","Content-Type":"application/json"}""",
                """{"paymentValue":{{paymentValue}},"debitAccount":{"branch":"{{debitBranch}}","number":"{{debitAccount}}"},"status":"AUTHORIZED"}""");

            SeedApiCall("Santander Pagamento - Callback pago", HttpMethodType.Post, "{{callbackBaseUrl}}/api/creatorpayments/provider-callback/{{callbackToken}}",
                """{"x-webhook-secret":"{{CallbackSecret}}","Content-Type":"application/json"}""",
                """{"provider":"santander","eventType":"paid","idempotencyKey":{{idempotencyKey | json}},"providerTransactionId":{{providerTransactionId | json}},"endToEndId":{{endToEndId | json}}}""");

            SeedApiCall("Santander Pagamento - Callback falha", HttpMethodType.Post, "{{callbackBaseUrl}}/api/creatorpayments/provider-callback/{{callbackToken}}",
                """{"x-webhook-secret":"{{CallbackSecret}}","Content-Type":"application/json"}""",
                """{"provider":"santander","eventType":"failed","idempotencyKey":{{idempotencyKey | json}},"failureReason":{{errorMessage | json}},"metadata":{{errorMessage | json}}}""");

            // --- Pipeline + Steps (2 passos: POST cria -> PATCH autoriza) ---
            SeedPipeline("santander-pagamento", "santander-pagamento-pix", "Pagar PIX", "Efetiva um repasse PIX por chave no Santander (POST cria + PATCH autoriza) e confirma via callback paid/failed.", isDefault: true, isTestPipeline: false, contractIdentifier: "pagamento.pix");
            SeedStep("santander-pagamento-pix", 1, "Normalizar pagamento", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "santander-pagamento-normalizar-pix");
            SeedStep("santander-pagamento-pix", 2, "Autenticar", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Santander Pagamento - Autenticacao");
            SeedStep("santander-pagamento-pix", 3, "Criar PIX", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Santander Pagamento - Criar PIX");
            SeedStep("santander-pagamento-pix", 4, "Extrair id", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "santander-pagamento-extrair-id");
            SeedStep("santander-pagamento-pix", 5, "Autorizar PIX", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Santander Pagamento - Autorizar PIX");
            SeedStep("santander-pagamento-pix", 6, "Normalizar resposta", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "santander-pagamento-normalizar-resposta");
            SeedStep("santander-pagamento-pix", 7, "Callback pago", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Santander Pagamento - Callback pago", ignoreOnResponse: true);
            SeedStep("santander-pagamento-pix", 8, "Callback falha", PipelineStepType.HttpRequest, ErrorAction.Continue, apiCall: "Santander Pagamento - Callback falha", ignoreOnResponse: true);

            // runOnError no callback de falha (o helper base SeedStep nao seta a coluna runonerror).
            Execute.Sql("""
                UPDATE pipelinestep SET runonerror = true, updatedat = now()
                WHERE apicallid = (SELECT id FROM apicall WHERE name = 'Santander Pagamento - Callback falha')
                  AND pipelineid IN (SELECT id FROM pipeline WHERE integrationid = (SELECT id FROM integration WHERE identifier = 'santander-pagamento'));
                """);
        }

        public override void Down()
        {
            // Catalogo/integracao de convergencia; Down nao reverte (connectors e execucoes referenciam estes registros).
        }
    }
}
