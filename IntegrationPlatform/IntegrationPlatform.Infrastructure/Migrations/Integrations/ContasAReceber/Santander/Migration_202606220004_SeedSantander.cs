using FluentMigrator;
using IntegrationPlatform.Domain.ValueObjects;
using IntegrationPlatform.Infrastructure.Migrations.Integrations;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations.ContasAReceber.Santander
{
    // Integracao Santander de cobranca (boleto + PIX, collection_bill_management v2) com mTLS (certificado A1).
    // Depende do catalogo contas-a-receber + contratos cobranca.criar/cancelar do SeedSicredi (versao menor) e
    // do contrato cobranca.consultar-titulo do SeedItau. Acrescenta o pipeline santander-consultar-titulo
    // (modo por-titulo: consulta boleto a boleto). Idempotente e convergente (INSERT WHERE NOT EXISTS por
    // chave natural), entao no banco onde Santander ja foi montado so o pipeline de consulta e inserido.
    [Migration(202606220004)]
    public sealed class Migration_202606220004_SeedSantander : IntegrationSeedMigration
    {
        public override void Up()
        {
            // Catalogo (idempotente; categoria + criar/cancelar ja vem do SeedSicredi, consultar-titulo do SeedItau).
            SeedCategory("contas-a-receber", "Contas a Receber", "Provedores de cobrança e recebíveis (boleto, PIX): registro, baixa e conciliação de liquidados.");

            SeedContract(
                identifier: "cobranca.consultar-titulo",
                name: "Consultar título",
                description: "Consulta a situação de UM boleto (modo por-título). O AgencyCampaign itera os boletos abertos.",
                categoryIdentifier: "contas-a-receber",
                inputSchema: """{ "nossoNumero": "string", "financialEntryId": "number" }""",
                outputSchema: """{ "situacao": "pago|aberto|baixado|devolvido", "paidAt": "string?", "amountPaid": "number", "financialEntryId": "string" }""",
                hasCallback: false,
                callbackSchema: null);

            // --- Integracao Santander ---
            SeedIntegration("santander", "Santander", "Cobrança bancária Santander (boleto + PIX) via API collection_bill_management v2, com mTLS.", "contas-a-receber", "https://logos.hunter.io/santander.com.br", supportsWebhook: true);

            SeedAttribute("santander", "client_id", "Client ID", FieldType.Text, required: true, order: 1, group: "Autenticação", sensitive: true, description: "Client ID da aplicação no developer.santander.com.br (também usado como X-Application-Key).");
            SeedAttribute("santander", "client_secret", "Client Secret", FieldType.Text, required: true, order: 2, group: "Autenticação", sensitive: true, description: "Client Secret da aplicação.");
            SeedAttribute("santander", "client_cert_pfx", "Certificado (.pfx)", FieldType.File, required: true, order: 3, group: "Certificado", sensitive: true, description: "Certificado A1 (.pfx) - mTLS OBRIGATÓRIO no Santander. Montar a partir do .crt + .key fornecidos.");
            SeedAttribute("santander", "client_cert_password", "Senha do certificado", FieldType.Text, required: true, order: 4, group: "Certificado", sensitive: true, description: "Senha do .pfx.");
            SeedAttribute("santander", "workspace_id", "Workspace ID", FieldType.Text, required: true, order: 5, group: "Beneficiário", description: "ID do workspace de cobrança (criado uma vez no Santander).");
            SeedAttribute("santander", "covenantCode", "Código do Convênio", FieldType.Text, required: true, order: 6, group: "Beneficiário", description: "Código do convênio de cobrança (covenantCode).");
            SeedAttribute("santander", "chave_pix", "Chave PIX (CNPJ)", FieldType.Text, required: false, order: 7, group: "Beneficiário", description: "Chave PIX (CNPJ) do beneficiário. Se preenchida, o boleto sai com PIX (key.dictKey). Vazia = sem PIX.");
            SeedAttribute("santander", "environment", "Ambiente", FieldType.Text, required: true, order: 8, group: "Endpoints", hidden: true, description: "PRODUCAO ou TESTE (campo environment do registro).", defaultValue: "PRODUCAO");
            SeedAttribute("santander", "base_url", "Base da API", FieldType.Text, required: true, order: 9, group: "Endpoints", hidden: true, description: "Base da API trust-open. Produção por padrão.", defaultValue: "https://trust-open.api.santander.com.br");
            SeedAttribute("santander", "callbackBaseUrl", "URL base do Mainstay (callback)", FieldType.Text, required: false, order: 10, group: "Webhook", hidden: true, description: "Base do AgencyCampaign que recebe os callbacks.", defaultValue: "https://agencias.mainstay.com.br");

            BindContract("santander", "cobranca.criar");
            BindContract("santander", "cobranca.cancelar");
            BindContract("santander", "cobranca.consultar-titulo");

            // --- JavaScript functions ---
            SeedJsFunction("santander-normalizar-payload",
                """
                function pad(s,n){s=''+(s==null?'':s);while(s.length<n){s='0'+s;}return s;}
                function san(s,n){s=(''+(s==null?'':s)).toUpperCase();var a='ÁÀÂÃÄÉÈÊËÍÌÎÏÓÒÔÕÖÚÙÛÜÇ',b='AAAAAEEEEIIIIOOOOOUUUUC',v='ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 &',r='';for(var i=0;i<s.length;i++){var c=s.charAt(i);var p=a.indexOf(c);if(p>=0){c=b.charAt(p);}r+=(v.indexOf(c)>=0)?c:' ';}while(r.indexOf('  ')>=0){r=r.replace('  ',' ');}return r.replace(/^ +| +$/g,'').substring(0,n);}
                var doc=(payload.payerDocument==null?'':(''+payload.payerDocument)).replace(/\D/g,'');
                var seu=(''+(payload.financialEntryId==null?'':payload.financialEntryId));
                var num=seu.replace(/\D/g,'')||'0';
                var bankNumber=pad(num.slice(-13),13);
                var nsuCode=pad(num.slice(-7),7);
                var hoje=new Date().toISOString().substring(0,10);
                var due=(payload.dueAt==null?'':(''+payload.dueAt)).substring(0,10);
                var cep=(payload.payerCep==null?'':(''+payload.payerCep)).replace(/\D/g,'');
                var zip=cep.length>=8?(cep.substring(0,5)+'-'+cep.substring(5,8)):cep;
                var addr=san((payload.payerStreet||'')+(payload.payerNumber?(', '+payload.payerNumber):''),40);
                var body={
                  nsuCode:nsuCode,
                  nsuDate:hoje,
                  environment:(attributes.environment||'PRODUCAO'),
                  covenantCode:(''+(attributes.covenantCode||'')),
                  payer:{
                    documentType:(doc.length>11?'CNPJ':'CPF'),
                    documentNumber:doc.substring(0,15),
                    name:san(payload.payerName,40),
                    address:addr,
                    neighborhood:san(payload.payerNeighborhood,30),
                    city:san(payload.payerCity,20),
                    state:san(payload.payerState,2),
                    zipCode:zip
                  },
                  bankNumber:bankNumber,
                  clientNumber:seu,
                  dueDate:due,
                  issueDate:hoje,
                  nominalValue:(Number(payload.amount)||0).toFixed(2),
                  documentKind:'BOLETO_DEPOSITO_APORTE',
                  paymentType:'REGISTRO'
                };
                var chave=(''+(attributes.chave_pix||''));
                if(chave){body.key={type:'CNPJ',dictKey:chave};}
                result.value={santBody:JSON.stringify(body),bankNumber:bankNumber};
                """,
                "Monta o body do registro Santander: nominalValue string 2 casas, datas yyyy-mm-dd, bankNumber(13)=nosso gerado, clientNumber=financialEntryId, key.dictKey=chave_pix (PIX). Normaliza textos.");

            SeedJsFunction("santander-normalizar-resposta",
                """
                result.value={
                  digitableLine:(variables.digitableLine||''),
                  qrCodePix:(variables.qrCodePix||'')
                };
                """,
                "Extrai digitableLine (linha digitável) e qrCodePix (copia-e-cola) da resposta do registro Santander.");

            SeedJsFunction("santander-normalizar-consulta",
                """
                function el(v){if(v==null){return null;}if(typeof v.length!=='number'&&typeof v.ToString==='function'){try{return JSON.parse(v.ToString());}catch(e){return v;}}return v;}
                function num(v){return Number(((v==null?'0':v)+'').replace(',','.'))||0;}
                var root=el(variables.data)||{};
                var status=((root.status||'')+'').toUpperCase();
                var amountPaid=0;
                var paidAt=null;
                if(status==='LIQUIDADO'){
                  var sd=(root.settlementData&&root.settlementData.length>0)?root.settlementData[0]:{};
                  amountPaid=(sd.settlementCreditedValue!=null)?num(sd.settlementCreditedValue):num(sd.settlementValue);
                  paidAt=sd.settlementDate||sd.settlementCreditDate||null;
                }else if(status==='BAIXADO'){
                  var wd=(root.writeOffData&&root.writeOffData.length>0)?root.writeOffData[0]:{};
                  amountPaid=num(wd.writeOffValue);
                  paidAt=wd.writeOffDate||null;
                }
                var situacao=(amountPaid>0&&paidAt)?'pago':((status.indexOf('DEVOLV')>=0)?'devolvido':((status==='BAIXADO')?'baixado':'aberto'));
                result.value={situacao:situacao,paidAt:paidAt,amountPaid:amountPaid,financialEntryId:(''+(variables.financialEntryId==null?'':variables.financialEntryId))};
                """,
                "Normaliza a consulta de liquidação Santander: status LIQUIDADO (código de barras -> settlementData) ou BAIXADO (PIX -> writeOffData) com valor>0 viram 'pago'; mapeia para { situacao, paidAt, amountPaid, financialEntryId }.");

            // --- ApiCalls ---
            SeedApiCall("Santander - Autenticação", HttpMethodType.Post, "{{base_url}}/auth/oauth/v2/token",
                """{"Content-Type":"application/x-www-form-urlencoded"}""",
                "client_id={{client_id}}&client_secret={{client_secret}}&grant_type=client_credentials");

            SeedApiCall("Santander - Registrar boleto", HttpMethodType.Post, "{{base_url}}/collection_bill_management/v2/workspaces/{{workspace_id}}/bank_slips",
                """{"Authorization":"Bearer {{access_token}}","X-Application-Key":"{{client_id}}","Content-Type":"application/json"}""",
                "{{santBody}}");

            SeedApiCall("Santander - Baixar boleto", HttpMethodType.Patch, "{{base_url}}/collection_bill_management/v2/workspaces/{{workspace_id}}/bank_slips",
                """{"Authorization":"Bearer {{access_token}}","X-Application-Key":"{{client_id}}","Content-Type":"application/json"}""",
                """{"covenantCode":"{{covenantCode}}","bankNumber":"{{chargeId}}","operation":"BAIXAR"}""");

            SeedApiCall("Santander - Consultar titulo", HttpMethodType.Get, "{{base_url}}/collection_bill_management/v2/bills/{{covenantCode}}.{{nossoNumero}}?tipoConsulta=settlement",
                """{"Authorization":"Bearer {{access_token}}","X-Application-Key":"{{client_id}}","Content-Type":"application/json"}""",
                "");

            SeedApiCall("Santander - Callback issued", HttpMethodType.Post, "{{callbackBaseUrl}}/api/financialentries/provider-callback/{{callbackToken}}",
                """{"x-webhook-secret":"{{CallbackSecret}}","Content-Type":"application/json"}""",
                """{"provider":"santander","eventType":"issued","financialEntryId":{{financialEntryId}},"chargeId":{{bankNumber | json}},"digitableLine":{{digitableLine | json}},"nossoNumero":{{bankNumber | json}},"pixCopyPaste":{{qrCodePix | json}}}""");

            SeedApiCall("Santander - Callback cancelled", HttpMethodType.Post, "{{callbackBaseUrl}}/api/financialentries/provider-callback/{{callbackToken}}",
                """{"x-webhook-secret":"{{CallbackSecret}}","Content-Type":"application/json"}""",
                """{"provider":"santander","eventType":"cancelled","chargeId":{{chargeId | json}},"financialEntryId":{{financialEntryId}}}""");

            SeedApiCall("Santander - Callback falha", HttpMethodType.Post, "{{callbackBaseUrl}}/api/financialentries/provider-callback/{{callbackToken}}",
                """{"x-webhook-secret":"{{CallbackSecret}}","Content-Type":"application/json"}""",
                """{"provider":"santander","eventType":"failed","financialEntryId":{{financialEntryId}},"metadata":{{errorMessage | json}}}""");

            // --- Pipelines + Steps ---
            SeedPipeline("santander", "santander-criar-cobranca", "Criar cobrança", "Registra boleto+PIX no Santander e dispara callback issued.", isDefault: true, isTestPipeline: false, contractIdentifier: "cobranca.criar");
            SeedStep("santander-criar-cobranca", 1, "Normalizar payload", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "santander-normalizar-payload");
            SeedStep("santander-criar-cobranca", 2, "Autenticar", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Santander - Autenticação");
            SeedStep("santander-criar-cobranca", 3, "Registrar boleto", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Santander - Registrar boleto");
            SeedStep("santander-criar-cobranca", 4, "Normalizar resposta", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "santander-normalizar-resposta");
            SeedStep("santander-criar-cobranca", 5, "Callback issued", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Santander - Callback issued", ignoreOnResponse: true);
            SeedStep("santander-criar-cobranca", 6, "Callback falha", PipelineStepType.HttpRequest, ErrorAction.Continue, apiCall: "Santander - Callback falha", ignoreOnResponse: true);

            SeedPipeline("santander", "santander-cancelar-cobranca", "Cancelar cobrança", "Comanda a baixa do boleto no Santander e dispara callback cancelled.", isDefault: true, isTestPipeline: false, contractIdentifier: "cobranca.cancelar");
            SeedStep("santander-cancelar-cobranca", 1, "Autenticar", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Santander - Autenticação");
            SeedStep("santander-cancelar-cobranca", 2, "Baixar boleto", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Santander - Baixar boleto");
            SeedStep("santander-cancelar-cobranca", 3, "Callback cancelled", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Santander - Callback cancelled", ignoreOnResponse: true);
            SeedStep("santander-cancelar-cobranca", 4, "Callback falha", PipelineStepType.HttpRequest, ErrorAction.Continue, apiCall: "Santander - Callback falha", ignoreOnResponse: true);

            SeedPipeline("santander", "santander-consultar-titulo", "Consultar título", "Consulta a liquidação de um boleto no Santander (settlement) e normaliza para conciliação (modo por-título).", isDefault: true, isTestPipeline: false, contractIdentifier: "cobranca.consultar-titulo");
            SeedStep("santander-consultar-titulo", 1, "Autenticar", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Santander - Autenticação");
            SeedStep("santander-consultar-titulo", 2, "Consultar titulo", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Santander - Consultar titulo");
            SeedStep("santander-consultar-titulo", 3, "Normalizar", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "santander-normalizar-consulta");

            // runOnError nos callbacks de falha (o helper base SeedStep nao seta a coluna runonerror).
            Execute.Sql("""
                UPDATE pipelinestep SET runonerror = true, updatedat = now()
                WHERE apicallid = (SELECT id FROM apicall WHERE name = 'Santander - Callback falha')
                  AND pipelineid IN (SELECT id FROM pipeline WHERE integrationid = (SELECT id FROM integration WHERE identifier = 'santander'));
                """);
        }

        public override void Down()
        {
            // Catalogo/integracao de convergencia; Down nao reverte (connectors e execucoes referenciam estes registros).
        }
    }
}
