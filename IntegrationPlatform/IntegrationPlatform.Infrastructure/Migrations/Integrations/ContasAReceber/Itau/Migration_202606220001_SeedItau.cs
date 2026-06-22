using FluentMigrator;
using IntegrationPlatform.Domain.ValueObjects;
using IntegrationPlatform.Infrastructure.Migrations.Integrations;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations.ContasAReceber.Itau
{
    // Integracao Itau de cobranca (boleto, Cash Management v2/v3) com mTLS (certificado A1).
    // Depende do catalogo contas-a-receber + contratos cobranca.criar/cancelar do SeedSicredi (versao menor).
    // Acrescenta o contrato cobranca.consultar-titulo (modo por-titulo). Idempotente e convergente.
    [Migration(202606220001)]
    public sealed class Migration_202606220001_SeedItau : IntegrationSeedMigration
    {
        public override void Up()
        {
            // Catalogo (idempotente; categoria + criar/cancelar ja vem do SeedSicredi).
            SeedCategory("contas-a-receber", "Contas a Receber", "Provedores de cobrança e recebíveis (boleto, PIX): registro, baixa e conciliação de liquidados.");

            SeedContract(
                identifier: "cobranca.consultar-titulo",
                name: "Consultar título",
                description: "Consulta a situação de UM boleto (modo por-título, ex.: Itaú). O AgencyCampaign itera os boletos abertos.",
                categoryIdentifier: "contas-a-receber",
                inputSchema: """{ "nossoNumero": "string", "financialEntryId": "number" }""",
                outputSchema: """{ "situacao": "pago|aberto|baixado|devolvido", "paidAt": "string?", "amountPaid": "number", "financialEntryId": "string" }""",
                hasCallback: false,
                callbackSchema: null);

            // --- Integracao Itau ---
            SeedIntegration("itau", "Itaú", "Cobrança bancária Itaú (boleto + PIX) — emissão e baixa via Cash Management v2 com mTLS (certificado A1).", "contas-a-receber", "https://upload.wikimedia.org/wikipedia/commons/8/8a/Banco_Ita%C3%BA_logo.svg", supportsWebhook: true);

            SeedAttribute("itau", "client_id", "Client ID", FieldType.Text, required: true, order: 1, group: "Autenticação", sensitive: true, description: "Client ID do app no devportal do Itaú (usado também como x-itau-apikey).");
            SeedAttribute("itau", "client_secret", "Client Secret", FieldType.Text, required: true, order: 2, group: "Autenticação", sensitive: true, description: "Client Secret do app no devportal do Itaú.");
            SeedAttribute("itau", "client_cert_pfx", "Certificado (.pfx)", FieldType.File, required: true, order: 3, group: "Certificado", sensitive: true, description: "Arquivo do certificado digital A1 (.pfx) usado para autenticar com o Itaú.");
            SeedAttribute("itau", "client_cert_password", "Senha do certificado", FieldType.Text, required: true, order: 4, group: "Certificado", sensitive: true, description: "Senha do arquivo .pfx.");
            SeedAttribute("itau", "id_beneficiario", "ID Beneficiário", FieldType.Text, required: true, order: 5, group: "Beneficiário", description: "Identificador da conta/convênio do beneficiário no Itaú.");
            SeedAttribute("itau", "codigo_carteira", "Código da Carteira", FieldType.Text, required: true, order: 6, group: "Beneficiário", description: "Código da carteira de cobrança (ex.: 109).");
            SeedAttribute("itau", "token_url", "URL de Token (OAuth)", FieldType.Text, required: true, order: 7, group: "Endpoints", hidden: true, description: "Endpoint de obtenção do access_token. Produção por padrão; troque para o host de homologação ao testar.", defaultValue: "https://sts.itau.com.br/api/oauth/token");
            SeedAttribute("itau", "api_base_emissao", "Base API Emissão/Baixa", FieldType.Text, required: true, order: 8, group: "Endpoints", hidden: true, description: "Base das chamadas de escrita (emissão e baixa). Produção por padrão.", defaultValue: "https://api.itau.com.br/cash_management/v2");
            SeedAttribute("itau", "api_base_consulta", "Base API Consulta", FieldType.Text, required: true, order: 9, group: "Endpoints", hidden: true, description: "Base das chamadas de leitura/consulta (liquidados — fase futura). Produção por padrão.", defaultValue: "https://secure.api.cloud.itau.com.br/boletoscash/v2");
            SeedAttribute("itau", "callbackBaseUrl", "URL base do Mainstay (callback)", FieldType.Text, required: false, order: 10, group: "Webhook", hidden: true, description: "Base do AgencyCampaign que recebe os callbacks de cobrança.", defaultValue: "https://agencias.mainstay.com.br");

            BindContract("itau", "cobranca.criar");
            BindContract("itau", "cobranca.cancelar");
            BindContract("itau", "cobranca.consultar-titulo");

            // --- JavaScript functions ---
            SeedJsFunction("itau-normalizar-payload",
                """
                function uuid(){return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g,function(c){var r=Math.random()*16|0,v=c==='x'?r:(r&0x3|0x8);return v.toString(16);});}
                function pad(s,n){s=''+(s==null?'':s);while(s.length<n){s='0'+s;}return s;}
                var doc=(payload.payerDocument==null?'':(''+payload.payerDocument)).replace(/[^0-9A-Za-z]/g,'').toUpperCase();
                var due=(payload.dueAt==null?'':(''+payload.dueAt)).substring(0,10);
                var cents=Math.round((Number(payload.amount)||0)*100);
                var valor=pad(cents,17);
                var hoje=new Date().toISOString().substring(0,10);
                var cep=(payload.payerCep==null?'':(''+payload.payerCep)).replace(/\D/g,'');
                var street=payload.payerStreet||'';
                var logradouro=street+(payload.payerNumber?(', '+payload.payerNumber):'');
                var seu=(''+(payload.financialEntryId==null?'':payload.financialEntryId)).substring(0,10);
                var nosso=pad((seu.replace(/\D/g,'')||'0').substring(0,8),8);
                var pessoa=(doc.length>11)?{codigo_tipo_pessoa:'J',numero_cadastro_nacional_pessoa_juridica:doc}:{codigo_tipo_pessoa:'F',numero_cadastro_pessoa_fisica:doc};
                var body={data:{
                  etapa_processo_boleto:'efetivacao',
                  codigo_canal_operacao:'API',
                  beneficiario:{id_beneficiario:(attributes.id_beneficiario||'')},
                  dado_boleto:{
                    descricao_instrumento_cobranca:'boleto',
                    tipo_boleto:'a vista',
                    codigo_carteira:(''+(attributes.codigo_carteira||'')),
                    codigo_especie:'01',
                    valor_abatimento:'00000000000000000',
                    data_emissao:hoje,
                    pagador:{
                      pessoa:{nome_pessoa:(payload.payerName||''),tipo_pessoa:pessoa},
                      endereco:{nome_logradouro:logradouro,nome_bairro:(payload.payerNeighborhood||''),nome_cidade:(payload.payerCity||''),sigla_UF:(payload.payerState||''),numero_CEP:cep}
                    },
                    dados_individuais_boleto:[{numero_nosso_numero:nosso,data_vencimento:due,valor_titulo:valor,texto_seu_numero:seu}],
                    desconto_expresso:false
                  }
                }};
                result.value={itauBody:JSON.stringify(body),correlationId:uuid(),flowId:uuid()};
                """,
                "Monta o body completo da emissão Itaú a partir do payload genérico (valor 17 dígitos, PF/PJ, texto_seu_numero=financialEntryId) e gera correlationId/flowId.");

            SeedJsFunction("itau-preparar-headers",
                """
                function uuid(){return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g,function(c){var r=Math.random()*16|0,v=c==='x'?r:(r&0x3|0x8);return v.toString(16);});}
                result.value={correlationId:uuid(),flowId:uuid()};
                """,
                "Gera correlationId/flowId (GUID) para os headers de rastreio do Itaú no fluxo de cancelamento.");

            SeedJsFunction("itau-normalizar-resposta",
                """
                function el(v){if(v==null){return null;}if(typeof v.length!=='number'&&typeof v.ToString==='function'){try{return JSON.parse(v.ToString());}catch(e){return v;}}return v;}
                function pad(s,n){s=''+(s==null?'':s);while(s.length<n){s='0'+s;}return s;}
                var root=el(variables.data)||{};
                var content=root.content||root;
                var db=content.dado_boleto||{};
                var ind=(db.dados_individuais_boleto&&db.dados_individuais_boleto[0])||{};
                var nosso=ind.numero_nosso_numero||'';
                var idBenef=(content.beneficiario&&content.beneficiario.id_beneficiario)||attributes.id_beneficiario||'';
                var carteira=pad((db.codigo_carteira||attributes.codigo_carteira||''),3);
                var idBoleto=idBenef+carteira+pad(nosso,8);
                result.value={
                  idBoleto:idBoleto,
                  idBoletoIndividual:(ind.id_boleto_individual||''),
                  nossoNumero:nosso,
                  linhaDigitavel:(ind.numero_linha_digitavel||''),
                  codigoBarras:(ind.codigo_barras||''),
                  pixCopyPaste:'',
                  txid:''
                };
                """,
                "Normaliza a resposta da emissão Itaú (schema confirmado pela doc oficial cash_management v2): lê data.content.dado_boleto.dados_individuais_boleto[0] -> numero_nosso_numero, numero_linha_digitavel, codigo_barras, id_boleto_individual. Constrói o idBoleto da baixa (id_beneficiario + codigo_carteira + nosso_numero = 23 dígitos) e o expõe como chargeId. PIX vazio (esta API é boleto puro; QR/copia-e-cola estão na doc de bolecode/PIX).");

            SeedJsFunction("itau-normalizar-consulta",
                """
                function el(v){if(v==null){return null;}if(typeof v.length!=='number'&&typeof v.ToString==='function'){try{return JSON.parse(v.ToString());}catch(e){return v;}}return v;}
                var root=el(variables.data);
                var first=(root&&typeof root.length==='number'&&root.length>0)?root[0]:(root||{});
                var db=first.dado_boleto||{};
                var ind=(db.dados_individuais_boleto&&db.dados_individuais_boleto[0])||{};
                var s=((ind.situacao_geral_boleto||'')+'').toLowerCase();
                var pagamentos=db.pagamentos_cobranca||[];
                var pag=(pagamentos&&pagamentos.length>0)?pagamentos[0]:{};
                var amountPaid=Number(((pag.valor_pago_total_cobranca||'0')+'').replace(',','.'))||0;
                var hasPayment=(pagamentos&&pagamentos.length>0)&&amountPaid>0;
                var situacao=hasPayment?'pago':(s.indexOf('devolv')>=0?'devolvido':(s.indexOf('baix')>=0?'baixado':(s.indexOf('pago')>=0?'pago':'aberto')));
                result.value={situacao:situacao,paidAt:(pag.data_inclusao_pagamento||null),amountPaid:amountPaid,financialEntryId:(ind.texto_seu_numero||'')};
                """,
                "Normaliza a Consulta de Detalhes do Título Itaú (data[].dado_boleto) para { situacao, paidAt, amountPaid, financialEntryId }. Pago detectado por pagamentos_cobranca (cobre boleto e PIX-QR).");

            // --- ApiCalls ---
            SeedApiCall("Itau - Autenticação", HttpMethodType.Post, " {{token_url}} ",
                """{"x-itau-flowID":"{{flowId}}","x-itau-correlationID":"{{correlationId}}","Content-Type":"application/x-www-form-urlencoded"}""",
                "grant_type=client_credentials&client_id={{client_id}}&client_secret={{client_secret}}");

            SeedApiCall("Itau - Emitir boleto", HttpMethodType.Post, "{{api_base_emissao}}/boletos",
                """{"x-itau-apikey":"{{client_id}}","x-itau-correlationID":"{{correlationId}}","x-itau-flowID":"{{flowId}}","Authorization":"Bearer {{access_token}}","Content-Type":"application/json"}""",
                "{{itauBody}}");

            SeedApiCall("Itau - Baixar boleto", HttpMethodType.Patch, "{{api_base_emissao}}/boletos/{{chargeId}}/baixa",
                """{"x-itau-apikey":"{{client_id}}","x-itau-correlationID":"{{correlationId}}","x-itau-flowID":"{{flowId}}","Authorization":"Bearer {{access_token}}","Content-Type":"application/json"}""",
                "{}");

            SeedApiCall("Itau - Consultar titulo", HttpMethodType.Get, "{{api_base_consulta}}/boletos?id_beneficiario={{id_beneficiario}}&codigo_carteira={{codigo_carteira}}&nosso_numero={{nossoNumero}}",
                """{"x-itau-apikey":"{{client_id}}","x-itau-correlationID":"{{correlationId}}","x-itau-flowID":"{{flowId}}","Authorization":"Bearer {{access_token}}","Content-Type":"application/json","view":"specific"}""",
                "");

            SeedApiCall("Itau - Callback issued", HttpMethodType.Post, "{{callbackBaseUrl}}/api/financialentries/provider-callback/{{callbackToken}}",
                """{"x-webhook-secret":"{{CallbackSecret}}","Content-Type":"application/json"}""",
                """{"provider":"itau","eventType":"issued","financialEntryId":{{financialEntryId}},"chargeId":{{idBoleto | json}},"digitableLine":{{linhaDigitavel | json}},"barCode":{{codigoBarras | json}},"nossoNumero":{{nossoNumero | json}},"pixCopyPaste":{{pixCopyPaste | json}},"txId":{{txid | json}}}""");

            SeedApiCall("Itau - Callback cancelled", HttpMethodType.Post, "{{callbackBaseUrl}}/api/financialentries/provider-callback/{{callbackToken}}",
                """{"x-webhook-secret":"{{CallbackSecret}}","Content-Type":"application/json"}""",
                """{"provider":"itau","eventType":"cancelled","chargeId":{{chargeId | json}},"financialEntryId":{{financialEntryId}}}""");

            SeedApiCall("Itau - Callback falha", HttpMethodType.Post, "{{callbackBaseUrl}}/api/financialentries/provider-callback/{{callbackToken}}",
                """{"x-webhook-secret":"{{CallbackSecret}}","Content-Type":"application/json"}""",
                """{"provider":"itau","eventType":"failed","financialEntryId":{{financialEntryId}},"metadata":{{errorMessage | json}}}""");

            // --- Pipelines + Steps ---
            SeedPipeline("itau", "itau-criar-cobranca", "Criar cobrança", "Emite boleto+PIX no Itaú e dispara callback issued (com id_boleto para correlação da baixa).", isDefault: true, isTestPipeline: false, contractIdentifier: "cobranca.criar");
            SeedStep("itau-criar-cobranca", 1, "Normalizar payload", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "itau-normalizar-payload");
            SeedStep("itau-criar-cobranca", 2, "Autenticar", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Itau - Autenticação");
            SeedStep("itau-criar-cobranca", 3, "Emitir boleto", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Itau - Emitir boleto");
            SeedStep("itau-criar-cobranca", 4, "Normalizar resposta", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "itau-normalizar-resposta");
            SeedStep("itau-criar-cobranca", 5, "Callback issued", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Itau - Callback issued", ignoreOnResponse: true);
            SeedStep("itau-criar-cobranca", 6, "Callback falha", PipelineStepType.HttpRequest, ErrorAction.Continue, apiCall: "Itau - Callback falha", ignoreOnResponse: true);

            SeedPipeline("itau", "itau-cancelar-cobranca", "Cancelar cobrança", "Comanda a baixa do boleto no Itaú (via id_boleto) e dispara callback cancelled.", isDefault: true, isTestPipeline: false, contractIdentifier: "cobranca.cancelar");
            SeedStep("itau-cancelar-cobranca", 1, "Preparar headers", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "itau-preparar-headers");
            SeedStep("itau-cancelar-cobranca", 2, "Autenticar", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Itau - Autenticação");
            SeedStep("itau-cancelar-cobranca", 3, "Baixar boleto", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Itau - Baixar boleto");
            SeedStep("itau-cancelar-cobranca", 4, "Callback cancelled", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Itau - Callback cancelled", ignoreOnResponse: true);
            SeedStep("itau-cancelar-cobranca", 5, "Callback falha", PipelineStepType.HttpRequest, ErrorAction.Continue, apiCall: "Itau - Callback falha", ignoreOnResponse: true);

            SeedPipeline("itau", "itau-consultar-titulo", "Consultar título", "Consulta a situação de um boleto no Itaú e normaliza para conciliação (modo por-título).", isDefault: true, isTestPipeline: false, contractIdentifier: "cobranca.consultar-titulo");
            SeedStep("itau-consultar-titulo", 1, "Preparar headers", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "itau-preparar-headers");
            SeedStep("itau-consultar-titulo", 2, "Autenticar", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Itau - Autenticação");
            SeedStep("itau-consultar-titulo", 3, "Consultar titulo", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "Itau - Consultar titulo");
            SeedStep("itau-consultar-titulo", 4, "Normalizar", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "itau-normalizar-consulta");

            // runOnError nos callbacks de falha (o helper base SeedStep nao seta a coluna runonerror).
            Execute.Sql("""
                UPDATE pipelinestep SET runonerror = true, updatedat = now()
                WHERE apicallid = (SELECT id FROM apicall WHERE name = 'Itau - Callback falha')
                  AND pipelineid IN (SELECT id FROM pipeline WHERE integrationid = (SELECT id FROM integration WHERE identifier = 'itau'));
                """);
        }

        public override void Down()
        {
            // Catalogo/integracao de convergencia; Down nao reverte (connectors e execucoes referenciam estes registros).
        }
    }
}
