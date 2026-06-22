using FluentMigrator;
using IntegrationPlatform.Domain.ValueObjects;
using IntegrationPlatform.Infrastructure.Migrations.Integrations;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations.ContasAReceber.Inter
{
    // Integracao Banco Inter de cobranca (boleto + PIX, API Cobranca v3) com mTLS. Emissao em 2 passos
    // (POST cria -> GET busca boleto+pix), liquidacao por PERIODO (reusa cobranca.consultar-liquidados).
    // Depende do catalogo + contratos cobranca.* do SeedSicredi (versao menor).
    [Migration(202606220003)]
    public sealed class Migration_202606220003_SeedInter : IntegrationSeedMigration
    {
        public override void Up()
        {
            SeedCategory("contas-a-receber", "Contas a Receber", "Provedores de cobrança e recebíveis (boleto, PIX): registro, baixa e conciliação de liquidados.");

            SeedIntegration("inter", "Banco Inter", "Cobrança bancária Banco Inter (boleto + PIX) via API Cobrança v3 com mTLS.", "contas-a-receber", "https://logos.hunter.io/inter.co", supportsWebhook: true);

            SeedAttribute("inter", "client_id", "Client ID", FieldType.Text, required: true, order: 1, group: "Autenticação", sensitive: true, description: "Client ID da aplicação criada no Internet Banking PJ do Inter.");
            SeedAttribute("inter", "client_secret", "Client Secret", FieldType.Text, required: true, order: 2, group: "Autenticação", sensitive: true, description: "Client Secret da aplicação Inter.");
            SeedAttribute("inter", "client_cert_pfx", "Certificado (.pfx)", FieldType.File, required: true, order: 3, group: "Certificado", sensitive: true, description: "Certificado A1 (.pfx) do Inter (mTLS obrigatório; validade 1 ano). Montar do .crt+.key.");
            SeedAttribute("inter", "client_cert_password", "Senha do certificado", FieldType.Text, required: true, order: 4, group: "Certificado", sensitive: true, description: "Senha do .pfx.");
            SeedAttribute("inter", "conta_corrente", "Conta Corrente", FieldType.Text, required: true, order: 5, group: "Beneficiário", description: "Número da conta corrente Inter (header x-conta-corrente).");
            SeedAttribute("inter", "scope", "Scopes OAuth", FieldType.Text, required: true, order: 6, group: "Autenticação", hidden: true, description: "Escopos do token.", defaultValue: "boleto-cobranca.read boleto-cobranca.write");
            SeedAttribute("inter", "token_url", "URL de Token (OAuth)", FieldType.Text, required: true, order: 7, group: "Endpoints", hidden: true, description: "Produção por padrão; sandbox = cdpj-sandbox.partners.uatinter.co/oauth/v2/token.", defaultValue: "https://cdpj.partners.bancointer.com.br/oauth/v2/token");
            SeedAttribute("inter", "api_base", "Base da API Cobrança", FieldType.Text, required: true, order: 8, group: "Endpoints", hidden: true, description: "Base da API Cobrança v3. Produção por padrão.", defaultValue: "https://cdpj.partners.bancointer.com.br/cobranca/v3");
            SeedAttribute("inter", "callbackBaseUrl", "URL base do Mainstay (callback)", FieldType.Text, required: false, order: 9, group: "Webhook", hidden: true, description: "Base do AgencyCampaign que recebe os callbacks.", defaultValue: "https://agencias.mainstay.com.br");

            BindContract("inter", "cobranca.criar");
            BindContract("inter", "cobranca.cancelar");
            BindContract("inter", "cobranca.consultar-liquidados");

            // --- JavaScript functions ---
            SeedJsFunction("inter-normalizar-payload",
                """
                var doc=(payload.payerDocument==null?'':(''+payload.payerDocument)).replace(/\D/g,'');
                var due=(payload.dueAt==null?'':(''+payload.dueAt)).substring(0,10);
                var cep=(payload.payerCep==null?'':(''+payload.payerCep)).replace(/\D/g,'');
                var body={
                  seuNumero:(''+(payload.financialEntryId==null?'':payload.financialEntryId)).substring(0,15),
                  valorNominal:(Number(payload.amount)||0),
                  dataVencimento:due,
                  numDiasAgenda:60,
                  pagador:{
                    cpfCnpj:doc,
                    tipoPessoa:(doc.length>11?'JURIDICA':'FISICA'),
                    nome:(payload.payerName||''),
                    endereco:(payload.payerStreet||''),
                    numero:(payload.payerNumber?(''+payload.payerNumber):'S/N'),
                    bairro:(payload.payerNeighborhood||''),
                    cidade:(payload.payerCity||''),
                    uf:(payload.payerState||''),
                    cep:cep
                  }
                };
                result.value={interBody:JSON.stringify(body)};
                """,
                "Monta o body do POST /cobrancas Inter v3 (seuNumero=financialEntryId, valorNominal decimal, dataVencimento yyyy-mm-dd, pagador PF/PJ).");

            SeedJsFunction("inter-normalizar-resposta",
                """
                function el(v){if(v==null){return null;}if(typeof v.length!=='number'&&typeof v.ToString==='function'){try{return JSON.parse(v.ToString());}catch(e){return v;}}return v;}
                var boleto=el(variables.boleto)||{};
                var pix=el(variables.pix)||{};
                if(!boleto.linhaDigitavel&&variables.cobranca){var c=el(variables.cobranca)||{};boleto=c.boleto||boleto;pix=c.pix||pix;}
                result.value={
                  linhaDigitavel:(boleto.linhaDigitavel||''),
                  codigoBarras:(boleto.codigoBarras||''),
                  nossoNumero:(boleto.nossoNumero||''),
                  pixCopyPaste:(pix.pixCopiaECola||''),
                  txid:(pix.txid||'')
                };
                """,
                "Extrai boleto{linhaDigitavel,codigoBarras,nossoNumero} e pix{pixCopiaECola,txid} da consulta da cobranca Inter.");

            SeedJsFunction("inter-preparar-consulta",
                """
                var p=(''+(payload.dia||'')).split('/');
                result.value={diaISO:(p.length===3?(p[2]+'-'+p[1]+'-'+p[0]):(''+(payload.dia||'')))};
                """,
                "Converte o dia (dd/MM/yyyy do AgencyCampaign) para yyyy-MM-dd (formato Inter).");

            SeedJsFunction("inter-normalizar-liquidados",
                """
                function el(v){if(v==null){return null;}if(typeof v.length!=='number'&&typeof v.ToString==='function'){try{return JSON.parse(v.ToString());}catch(e){return v;}}return v;}
                var raw=variables.cobrancas;
                var items=[];
                if(raw!=null){if(typeof raw.length==='number'){items=raw;}else if(typeof raw.ToString==='function'){try{items=JSON.parse(raw.ToString());}catch(e){items=[];}}}
                var out=[];
                for(var i=0;i<items.length;i++){
                  var it=items[i];
                  var c=it.cobranca||it;
                  var fid=(''+(c.seuNumero||''));
                  if(fid){
                    out.push({financialEntryId:fid,providerChargeId:(''+(c.codigoSolicitacao||'')),paidAt:(c.dataSituacao||c.dataHoraSituacao||null),amountPaid:(Number(c.valorTotalRecebido||c.valorNominal||0))});
                  }
                }
                result.value={liquidados:out};
                """,
                "Normaliza a lista de cobrancas RECEBIDAS do Inter para { liquidados:[...] }: financialEntryId=seuNumero, providerChargeId=codigoSolicitacao.");

            // --- ApiCalls ---
            SeedApiCall("Inter - Autenticação", HttpMethodType.Post, "{{token_url}}",
                """{"Content-Type":"application/x-www-form-urlencoded"}""",
                "client_id={{client_id}}&client_secret={{client_secret}}&grant_type=client_credentials&scope={{scope}}");

            SeedApiCall("Inter - Criar cobrança", HttpMethodType.Post, "{{api_base}}/cobrancas",
                """{"Authorization":"Bearer {{access_token}}","x-conta-corrente":"{{conta_corrente}}","Content-Type":"application/json"}""",
                "{{interBody}}");

            SeedApiCall("Inter - Consultar cobrança", HttpMethodType.Get, "{{api_base}}/cobrancas/{{codigoSolicitacao}}",
                """{"Authorization":"Bearer {{access_token}}","x-conta-corrente":"{{conta_corrente}}"}""",
                "");

            SeedApiCall("Inter - Cancelar cobrança", HttpMethodType.Post, "{{api_base}}/cobrancas/{{chargeId}}/cancelar",
                """{"Authorization":"Bearer {{access_token}}","x-conta-corrente":"{{conta_corrente}}","Content-Type":"application/json"}""",
                """{"motivoCancelamento":"ACERTOS"}""");

            SeedApiCall("Inter - Consultar liquidados", HttpMethodType.Get, "{{api_base}}/cobrancas?dataInicial={{diaISO}}&dataFinal={{diaISO}}&filtrarDataPor=PAGAMENTO&situacao=RECEBIDO&itensPorPagina=100",
                """{"Authorization":"Bearer {{access_token}}","x-conta-corrente":"{{conta_corrente}}"}""",
                "");

            SeedApiCall("Inter - Callback issued", HttpMethodType.Post, "{{callbackBaseUrl}}/api/financialentries/provider-callback/{{callbackToken}}",
                """{"x-webhook-secret":"{{CallbackSecret}}","Content-Type":"application/json"}""",
                """{"provider":"inter","eventType":"issued","financialEntryId":{{financialEntryId}},"chargeId":{{codigoSolicitacao | json}},"digitableLine":{{linhaDigitavel | json}},"barCode":{{codigoBarras | json}},"nossoNumero":{{nossoNumero | json}},"pixCopyPaste":{{pixCopyPaste | json}},"txId":{{txid | json}}}""");

            SeedApiCall("Inter - Callback cancelled", HttpMethodType.Post, "{{callbackBaseUrl}}/api/financialentries/provider-callback/{{callbackToken}}",
                """{"x-webhook-secret":"{{CallbackSecret}}","Content-Type":"application/json"}""",
                """{"provider":"inter","eventType":"cancelled","chargeId":{{chargeId | json}},"financialEntryId":{{financialEntryId}}}""");

            SeedApiCall("Inter - Callback falha", HttpMethodType.Post, "{{callbackBaseUrl}}/api/financialentries/provider-callback/{{callbackToken}}",
                """{"x-webhook-secret":"{{CallbackSecret}}","Content-Type":"application/json"}""",
                """{"provider":"inter","eventType":"failed","financialEntryId":{{financialEntryId}},"metadata":{{errorMessage | json}}}""");

            // --- Pipelines + Steps ---
            SeedPipeline("inter", "inter-criar-cobranca", "Criar cobrança", "Cria a cobrança (POST), consulta os dados (GET) e dispara callback issued.", isDefault: true, isTestPipeline: false, contractIdentifier: "cobranca.criar");
            SeedStep("inter-criar-cobranca", 1, "Normalizar payload", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "inter-normalizar-payload");
            SeedStep("inter-criar-cobranca", 2, "Autenticar", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Inter - Autenticação");
            SeedStep("inter-criar-cobranca", 3, "Criar cobrança", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Inter - Criar cobrança");
            SeedStep("inter-criar-cobranca", 4, "Consultar cobrança", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Inter - Consultar cobrança");
            SeedStep("inter-criar-cobranca", 5, "Normalizar resposta", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "inter-normalizar-resposta");
            SeedStep("inter-criar-cobranca", 6, "Callback issued", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Inter - Callback issued", ignoreOnResponse: true);
            SeedStep("inter-criar-cobranca", 7, "Callback falha", PipelineStepType.HttpRequest, ErrorAction.Continue, apiCall: "Inter - Callback falha", ignoreOnResponse: true);

            SeedPipeline("inter", "inter-cancelar-cobranca", "Cancelar cobrança", "Cancela a cobrança no Inter e dispara callback cancelled.", isDefault: true, isTestPipeline: false, contractIdentifier: "cobranca.cancelar");
            SeedStep("inter-cancelar-cobranca", 1, "Autenticar", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Inter - Autenticação");
            SeedStep("inter-cancelar-cobranca", 2, "Cancelar cobrança", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Inter - Cancelar cobrança");
            SeedStep("inter-cancelar-cobranca", 3, "Callback cancelled", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Inter - Callback cancelled", ignoreOnResponse: true);
            SeedStep("inter-cancelar-cobranca", 4, "Callback falha", PipelineStepType.HttpRequest, ErrorAction.Continue, apiCall: "Inter - Callback falha", ignoreOnResponse: true);

            SeedPipeline("inter", "inter-consultar-liquidados", "Consultar liquidados", "Lista as cobranças RECEBIDAS do dia (modo período) e normaliza para conciliação.", isDefault: true, isTestPipeline: false, contractIdentifier: "cobranca.consultar-liquidados");
            SeedStep("inter-consultar-liquidados", 1, "Preparar consulta", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "inter-preparar-consulta");
            SeedStep("inter-consultar-liquidados", 2, "Autenticar", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Inter - Autenticação");
            SeedStep("inter-consultar-liquidados", 3, "Consultar liquidados", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Inter - Consultar liquidados");
            SeedStep("inter-consultar-liquidados", 4, "Normalizar", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "inter-normalizar-liquidados");

            // runOnError nos callbacks de falha (o helper base SeedStep nao seta a coluna runonerror).
            Execute.Sql("""
                UPDATE pipelinestep SET runonerror = true, updatedat = now()
                WHERE apicallid = (SELECT id FROM apicall WHERE name = 'Inter - Callback falha')
                  AND pipelineid IN (SELECT id FROM pipeline WHERE integrationid = (SELECT id FROM integration WHERE identifier = 'inter'));
                """);
        }

        public override void Down()
        {
            // Catalogo/integracao de convergencia; Down nao reverte (connectors e execucoes referenciam estes registros).
        }
    }
}
