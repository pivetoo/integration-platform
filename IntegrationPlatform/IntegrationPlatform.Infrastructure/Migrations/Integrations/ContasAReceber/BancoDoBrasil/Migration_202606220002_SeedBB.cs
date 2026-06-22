using FluentMigrator;
using IntegrationPlatform.Domain.ValueObjects;
using IntegrationPlatform.Infrastructure.Migrations.Integrations;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations.ContasAReceber.BancoDoBrasil
{
    // Integracao Banco do Brasil de cobranca (boleto + PIX, API Cobrancas v2). Auth OAuth client_credentials
    // (Basic) + app-key; PIX nativo; liquidacao por PERIODO (reusa cobranca.consultar-liquidados do Sicredi).
    // Depende do catalogo + contratos cobranca.criar/cancelar/consultar-liquidados do SeedSicredi (versao menor).
    [Migration(202606220002)]
    public sealed class Migration_202606220002_SeedBB : IntegrationSeedMigration
    {
        public override void Up()
        {
            SeedCategory("contas-a-receber", "Contas a Receber", "Provedores de cobrança e recebíveis (boleto, PIX): registro, baixa e conciliação de liquidados.");

            SeedIntegration("bb", "Banco do Brasil", "Cobrança bancária Banco do Brasil (boleto + PIX) via API Cobranças v2.", "contas-a-receber", "https://logos.hunter.io/bb.com.br", supportsWebhook: true);

            SeedAttribute("bb", "basic_auth", "Credencial Basic", FieldType.Text, required: true, order: 1, group: "Autenticação", sensitive: true, description: "Base64 de client_id:client_secret (gerado no developer.bb.com.br). Vai no header Authorization: Basic.");
            SeedAttribute("bb", "app_key", "Chave de Aplicação (app-key)", FieldType.Text, required: true, order: 2, group: "Autenticação", sensitive: true, description: "gw-dev-app-key da aplicação no developer.bb.com.br. Enviada como query em toda chamada de API.");
            SeedAttribute("bb", "scope", "Scopes OAuth", FieldType.Text, required: true, order: 3, group: "Autenticação", hidden: true, description: "Escopos do token.", defaultValue: "cobrancas.boletos-requisicao cobrancas.boletos-info cobrancas.convenio-requisicao");
            SeedAttribute("bb", "client_cert_pfx", "Certificado (.pfx)", FieldType.File, required: false, order: 4, group: "Certificado", sensitive: true, description: "Opcional: certificado A1 (.pfx) se o convênio exigir mTLS.");
            SeedAttribute("bb", "client_cert_password", "Senha do certificado", FieldType.Text, required: false, order: 5, group: "Certificado", sensitive: true, description: "Opcional: senha do .pfx (se usar mTLS).");
            SeedAttribute("bb", "numeroConvenio", "Número do Convênio", FieldType.Text, required: true, order: 6, group: "Beneficiário", description: "Número do convênio de cobrança.");
            SeedAttribute("bb", "numeroCarteira", "Número da Carteira", FieldType.Text, required: true, order: 7, group: "Beneficiário", description: "Carteira do convênio (ex.: 17).");
            SeedAttribute("bb", "numeroVariacaoCarteira", "Variação da Carteira", FieldType.Text, required: true, order: 8, group: "Beneficiário", description: "Variação da carteira (ex.: 35).");
            SeedAttribute("bb", "agencia", "Agência", FieldType.Text, required: true, order: 9, group: "Beneficiário", description: "Agência do beneficiário, sem o dígito (usada na consulta por período).");
            SeedAttribute("bb", "conta", "Conta", FieldType.Text, required: true, order: 10, group: "Beneficiário", description: "Conta corrente do beneficiário, sem o dígito (usada na consulta por período).");
            SeedAttribute("bb", "token_url", "URL de Token (OAuth)", FieldType.Text, required: true, order: 11, group: "Endpoints", hidden: true, description: "Produção por padrão; trocar p/ homologação (oauth.hm.bb.com.br) ao testar.", defaultValue: "https://oauth.bb.com.br/oauth/token");
            SeedAttribute("bb", "api_base", "Base da API", FieldType.Text, required: true, order: 12, group: "Endpoints", hidden: true, description: "Base da API de Cobranças. Produção por padrão.", defaultValue: "https://api.bb.com.br/cobrancas/v2");
            SeedAttribute("bb", "callbackBaseUrl", "URL base do Mainstay (callback)", FieldType.Text, required: false, order: 13, group: "Webhook", hidden: true, description: "Base do AgencyCampaign que recebe os callbacks.", defaultValue: "https://agencias.mainstay.com.br");

            BindContract("bb", "cobranca.criar");
            BindContract("bb", "cobranca.cancelar");
            BindContract("bb", "cobranca.consultar-liquidados");

            // --- JavaScript functions ---
            SeedJsFunction("bb-normalizar-payload",
                """
                function pad(s,n){s=''+(s==null?'':s);while(s.length<n){s='0'+s;}return s;}
                function brdate(s){s=(s==null?'':(''+s)).substring(0,10);var p=s.split('-');return (p.length===3)?(p[2]+'.'+p[1]+'.'+p[0]):s;}
                var doc=(payload.payerDocument==null?'':(''+payload.payerDocument)).replace(/\D/g,'');
                var convenio=(''+(attributes.numeroConvenio||'')).replace(/\D/g,'');
                var seu=(''+(payload.financialEntryId==null?'':payload.financialEntryId));
                var nosso=pad(seu.replace(/\D/g,'').slice(-10),10);
                var numeroTituloCliente='000'+pad(convenio,7)+nosso;
                var method=(payload.method==null?'':(''+payload.method)).toLowerCase();
                var cep=(payload.payerCep==null?'':(''+payload.payerCep)).replace(/\D/g,'');
                var body={
                  numeroConvenio:Number(convenio),
                  numeroCarteira:Number(attributes.numeroCarteira||0),
                  numeroVariacaoCarteira:Number(attributes.numeroVariacaoCarteira||0),
                  codigoModalidade:1,
                  dataEmissao:brdate(new Date().toISOString()),
                  dataVencimento:brdate(payload.dueAt),
                  valorOriginal:Number(payload.amount)||0,
                  valorAbatimento:0,
                  codigoAceite:'N',
                  codigoTipoTitulo:2,
                  descricaoTipoTitulo:'DM',
                  indicadorPermissaoRecebimentoParcial:'N',
                  numeroTituloBeneficiario:seu.substring(0,15),
                  numeroTituloCliente:numeroTituloCliente,
                  indicadorPix:(method==='pix'?'S':'N'),
                  pagador:{
                    tipoInscricao:(doc.length>11?2:1),
                    numeroInscricao:doc,
                    nome:(payload.payerName||''),
                    endereco:((payload.payerStreet||'')+(payload.payerNumber?(', '+payload.payerNumber):'')),
                    cep:(Number(cep)||0),
                    cidade:(payload.payerCity||''),
                    bairro:(payload.payerNeighborhood||''),
                    uf:(payload.payerState||'')
                  }
                };
                result.value={bbBody:JSON.stringify(body),numeroBoleto:numeroTituloCliente};
                """,
                "Monta o body do registro BB (valor decimal, datas dd.mm.yyyy, numeroTituloCliente=000+convenio(7)+nosso(10), indicadorPix por method). Expoe numeroBoleto p/ correlacao.");

            SeedJsFunction("bb-normalizar-resposta",
                """
                function el(v){if(v==null){return null;}if(typeof v.length!=='number'&&typeof v.ToString==='function'){try{return JSON.parse(v.ToString());}catch(e){return v;}}return v;}
                var qr=el(variables.qrCode)||{};
                result.value={
                  linhaDigitavel:(variables.linhaDigitavel||''),
                  codigoBarras:(variables.codigoBarraNumerico||''),
                  pixCopyPaste:(qr.emv||''),
                  txid:(qr.txId||'')
                };
                """,
                "Extrai linha digitavel, codigo de barras e PIX (qrCode.emv/txId) da resposta do registro BB.");

            SeedJsFunction("bb-preparar-consulta",
                """
                var d=(''+(payload.dia||'')).replace(/\//g,'.');
                result.value={diaBB:d};
                """,
                "Converte o dia (dd/MM/yyyy do AgencyCampaign) para o formato BB dd.mm.yyyy.");

            SeedJsFunction("bb-normalizar-liquidados",
                """
                function el(v){if(v==null){return null;}if(typeof v.length!=='number'&&typeof v.ToString==='function'){try{return JSON.parse(v.ToString());}catch(e){return v;}}return v;}
                function iso(s){var p=(''+(s==null?'':s)).substring(0,10).split('.');return (p.length===3)?(p[2]+'-'+p[1]+'-'+p[0]):s;}
                var raw=variables.boletos;
                var items=[];
                if(raw!=null){if(typeof raw.length==='number'){items=raw;}else if(typeof raw.ToString==='function'){try{items=JSON.parse(raw.ToString());}catch(e){items=[];}}}
                var out=[];
                for(var i=0;i<items.length;i++){
                  var it=items[i];
                  var pago=Number(it.valorPago||0);
                  if(pago>0){
                    var nb=(''+(it.numeroBoletoBB||''));
                    var fid=parseInt(nb.slice(-10),10);
                    out.push({financialEntryId:(fid?(''+fid):''),providerChargeId:nb,paidAt:iso(it.dataMovimento||it.dataCredito),amountPaid:pago});
                  }
                }
                result.value={liquidados:out};
                """,
                "Normaliza a lista de boletos BB (situacao B) para { liquidados:[...] }: filtra valorPago>0, financialEntryId vem dos ultimos 10 digitos do numeroBoletoBB.");

            // --- ApiCalls ---
            SeedApiCall("BB - Autenticação", HttpMethodType.Post, "{{token_url}}",
                """{"Authorization":"Basic {{basic_auth}}","Content-Type":"application/x-www-form-urlencoded"}""",
                "grant_type=client_credentials&scope={{scope}}");

            SeedApiCall("BB - Registrar boleto", HttpMethodType.Post, "{{api_base}}/boletos?gw-dev-app-key={{app_key}}",
                """{"Authorization":"Bearer {{access_token}}","Content-Type":"application/json"}""",
                "{{bbBody}}");

            SeedApiCall("BB - Callback issued", HttpMethodType.Post, "{{callbackBaseUrl}}/api/financialentries/provider-callback/{{callbackToken}}",
                """{"x-webhook-secret":"{{CallbackSecret}}","Content-Type":"application/json"}""",
                """{"provider":"bb","eventType":"issued","financialEntryId":{{financialEntryId}},"chargeId":{{numeroBoleto | json}},"digitableLine":{{linhaDigitavel | json}},"barCode":{{codigoBarras | json}},"nossoNumero":{{numeroBoleto | json}},"pixCopyPaste":{{pixCopyPaste | json}},"txId":{{txid | json}}}""");

            SeedApiCall("BB - Baixar boleto", HttpMethodType.Post, "{{api_base}}/boletos/{{chargeId}}/baixar?gw-dev-app-key={{app_key}}",
                """{"Authorization":"Bearer {{access_token}}","Content-Type":"application/json"}""",
                """{"numeroConvenio":{{numeroConvenio}}}""");

            SeedApiCall("BB - Callback cancelled", HttpMethodType.Post, "{{callbackBaseUrl}}/api/financialentries/provider-callback/{{callbackToken}}",
                """{"x-webhook-secret":"{{CallbackSecret}}","Content-Type":"application/json"}""",
                """{"provider":"bb","eventType":"cancelled","chargeId":{{chargeId | json}},"financialEntryId":{{financialEntryId}}}""");

            SeedApiCall("BB - Callback falha", HttpMethodType.Post, "{{callbackBaseUrl}}/api/financialentries/provider-callback/{{callbackToken}}",
                """{"x-webhook-secret":"{{CallbackSecret}}","Content-Type":"application/json"}""",
                """{"provider":"bb","eventType":"failed","financialEntryId":{{financialEntryId}},"metadata":{{errorMessage | json}}}""");

            SeedApiCall("BB - Consultar liquidados", HttpMethodType.Get, "{{api_base}}/boletos?gw-dev-app-key={{app_key}}&indicadorSituacao=B&agenciaBeneficiario={{agencia}}&contaBeneficiario={{conta}}&dataInicioMovimento={{diaBB}}&dataFimMovimento={{diaBB}}",
                """{"Authorization":"Bearer {{access_token}}"}""",
                "");

            // --- Pipelines + Steps ---
            SeedPipeline("bb", "bb-criar-cobranca", "Criar cobrança", "Registra boleto (+PIX) no BB e dispara callback issued.", isDefault: true, isTestPipeline: false, contractIdentifier: "cobranca.criar");
            SeedStep("bb-criar-cobranca", 1, "Normalizar payload", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "bb-normalizar-payload");
            SeedStep("bb-criar-cobranca", 2, "Autenticar", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "BB - Autenticação");
            SeedStep("bb-criar-cobranca", 3, "Registrar boleto", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "BB - Registrar boleto");
            SeedStep("bb-criar-cobranca", 4, "Normalizar resposta", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "bb-normalizar-resposta");
            SeedStep("bb-criar-cobranca", 5, "Callback issued", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "BB - Callback issued", ignoreOnResponse: true);
            SeedStep("bb-criar-cobranca", 6, "Callback falha", PipelineStepType.HttpRequest, ErrorAction.Continue, apiCall: "BB - Callback falha", ignoreOnResponse: true);

            SeedPipeline("bb", "bb-cancelar-cobranca", "Cancelar cobrança", "Baixa o boleto no BB e dispara callback cancelled.", isDefault: true, isTestPipeline: false, contractIdentifier: "cobranca.cancelar");
            SeedStep("bb-cancelar-cobranca", 1, "Autenticar", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "BB - Autenticação");
            SeedStep("bb-cancelar-cobranca", 2, "Baixar boleto", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "BB - Baixar boleto");
            SeedStep("bb-cancelar-cobranca", 3, "Callback cancelled", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "BB - Callback cancelled", ignoreOnResponse: true);
            SeedStep("bb-cancelar-cobranca", 4, "Callback falha", PipelineStepType.HttpRequest, ErrorAction.Continue, apiCall: "BB - Callback falha", ignoreOnResponse: true);

            SeedPipeline("bb", "bb-consultar-liquidados", "Consultar liquidados", "Lista os boletos liquidados do dia no BB (modo período) e normaliza para conciliação.", isDefault: true, isTestPipeline: false, contractIdentifier: "cobranca.consultar-liquidados");
            SeedStep("bb-consultar-liquidados", 1, "Preparar consulta", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "bb-preparar-consulta");
            SeedStep("bb-consultar-liquidados", 2, "Autenticar", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "BB - Autenticação");
            SeedStep("bb-consultar-liquidados", 3, "Consultar liquidados", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "BB - Consultar liquidados");
            SeedStep("bb-consultar-liquidados", 4, "Normalizar", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "bb-normalizar-liquidados");

            // runOnError nos callbacks de falha (o helper base SeedStep nao seta a coluna runonerror).
            Execute.Sql("""
                UPDATE pipelinestep SET runonerror = true, updatedat = now()
                WHERE apicallid = (SELECT id FROM apicall WHERE name = 'BB - Callback falha')
                  AND pipelineid IN (SELECT id FROM pipeline WHERE integrationid = (SELECT id FROM integration WHERE identifier = 'bb'));
                """);
        }

        public override void Down()
        {
            // Catalogo/integracao de convergencia; Down nao reverte (connectors e execucoes referenciam estes registros).
        }
    }
}
