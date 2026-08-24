using FluentMigrator;
using IntegrationPlatform.Domain.ValueObjects;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations.AssinaturaDigital.ClickSign
{
    // Integracao ClickSign de assinatura digital (API v3 "Envelope", JSON:API, header Authorization
    // com o token cru - sem "Bearer ").
    // Decisoes de desenho (diferente da ZapSign/D4Sign):
    // - ClickSign NAO devolve um sign_url utilizavel pelo Mainstay em nenhuma chamada do fluxo padrao:
    //   o link de assinatura e enviado pela PROPRIA ClickSign direto ao signatario (e-mail/sms/whatsapp,
    //   via communicate_events no signatario + POST /notifications). A alternativa (widget embarcado
    //   JS+iframe) exigiria mudanca de frontend fora do escopo desta integracao - decisao do usuario foi
    //   aceitar a notificacao nativa da ClickSign (callback "sent" vai sem signingUrl; o backend ja tolera
    //   isso - CampaignDocumentService so chama AssignSignerSigningUrl quando SigningUrl vem preenchido).
    // - Fluxo em varias chamadas sequenciais (a API v3 nao cria documento+signatarios numa chamada so como
    //   a ZapSign): criar envelope -> criar documento -> por signatario (ate 3, unrolled com runcondition
    //   igual ao padrao ZapSign) criar signatario -> requisito de qualificacao (action=agree, role=sign) ->
    //   requisito de autenticacao (action=provide_evidence, auth=email) -> ativar envelope (status=running,
    //   exige pelo menos 1 requisito de autenticacao por signatario) -> notificar (dispara o envio nativo).
    // - Cada chamada HTTP devolve JSON:API ({"data":{"id":...,"attributes":{...}}}); o motor do IP
    //   desembrulha automaticamente a chave unica "data" (EnvelopeKeys em ExecutionEngineService), entao
    //   "id" fica disponivel direto em variables apos cada chamada. Como cada chamada SOBRESCREVE
    //   variables.id, um step JS minusculo logo depois de cada criacao (envelope/documento/signatario)
    //   copia pro nome estavel (envelopeId/documentId/signerNId) antes do proximo HTTP apagar.
    // - Correlacao do webhook por Provider+ProviderDocumentId (ProviderDocumentId = ClickSign document id,
    //   nao o envelope id) - mesmo mecanismo ja usado por ZapSign/D4Sign, sem depender do campo opcional
    //   "metadata" do documento (nao confirmado com teste real ainda).
    // - Setup por tenant: gerar o Access Token em Configuracoes > API na conta ClickSign (sandbox:
    //   https://sandbox.clicksign.com/signup) e cadastrar o webhook (Configuracoes > API > Webhooks) com
    //   os eventos "sign" e "refusal" apontando para
    //   https://integrations.mainstay.com.br/api/webhooks/{tenantId}/clicksign.
    [Migration(202608240003)]
    public sealed class Migration_202608240003_SeedClickSign : IntegrationSeedMigration
    {
        public override void Up()
        {
            // --- Integracao ClickSign (categoria e contrato "assinatura-digital"/"assinatura.enviar" ja
            // existem, seedados pela ZapSign) ---
            SeedIntegration("clicksign", "ClickSign", "Assinatura digital via ClickSign (API v3, Envelope): envio de PDF com signatários e notificação nativa da ClickSign por e-mail. Cadastre na ClickSign o webhook (eventos sign e refusal) apontando para https://integrations.mainstay.com.br/api/webhooks/{tenantId}/clicksign.", "assinatura-digital", "https://logos.hunter.io/clicksign.com", supportsWebhook: true);

            SeedAttribute("clicksign", "api_token", "Access Token", FieldType.Text, required: true, order: 1, group: "Autenticação", sensitive: true, description: "Access Token gerado em Configurações > API da conta ClickSign (header Authorization, sem prefixo Bearer).");
            SeedAttribute("clicksign", "base_url", "Base da API", FieldType.Text, required: true, order: 2, group: "Endpoints", hidden: true, description: "Base da API v3. Produção por padrão; sandbox = https://sandbox.clicksign.com/api/v3.", defaultValue: "https://app.clicksign.com/api/v3");
            SeedAttribute("clicksign", "callbackBaseUrl", "URL base do Mainstay (callback)", FieldType.Text, required: false, order: 3, group: "Webhook", hidden: true, description: "Base do AgencyCampaign que recebe os callbacks.", defaultValue: "https://agencias.mainstay.com.br");

            BindContract("clicksign", "assinatura.enviar");

            // --- JavaScript functions ---
            SeedJsFunction("clicksign-normalizar-envio",
                """
                function s(v){return v==null?'':(''+v);}
                function el(v){if(v==null){return [];}if(typeof v.length!=='number'&&typeof v.ToString==='function'){try{return JSON.parse(v.ToString());}catch(e){return [];}}return v;}
                function signerBody(sig){
                  if(!sig){return '';}
                  var attrs={name:s(sig.name),email:s(sig.email),communicate_events:{signature_request:'email',signature_reminder:'email',document_signed:'email'}};
                  var doc=s(sig.documentNumber);
                  if(doc){attrs.documentation=doc;attrs.has_documentation=true;}else{attrs.has_documentation=false;}
                  return JSON.stringify({data:{type:'signers',attributes:attrs}});
                }
                var signers=el(payload.signers);
                var out={};
                out.title=s(payload.title)||'Documento';
                out.documentBase64=s(payload.documentBase64);
                out.documentName=s(payload.documentName)||'documento.pdf';
                out.message=s(payload.message);
                out.callbackToken=s(payload.callbackToken);
                out.campaignDocumentId=s(payload.campaignDocumentId);
                out.signer1Body=signerBody(signers[0]);
                out.signer1Email=signers[0]?s(signers[0].email):'';
                out.hasSigner2=!!signers[1];
                out.signer2Body=signerBody(signers[1]);
                out.signer2Email=signers[1]?s(signers[1].email):'';
                out.hasSigner3=!!signers[2];
                out.signer3Body=signerBody(signers[2]);
                out.signer3Email=signers[2]?s(signers[2].email):'';
                result.value=out;
                """,
                "Normaliza o payload de envio: desembrulha payload.signers (ate 3, el()) e monta o body JSON:API pronto de cada signatário (documentation/has_documentation condicional) mais os campos escalares do envelope/documento.");

            SeedJsFunction("clicksign-guardar-envelope-id", "result.value={envelopeId:(''+(variables.id||''))};", "Copia variables.id (resposta de Criar envelope) para envelopeId antes do próximo passo sobrescrever.");
            SeedJsFunction("clicksign-guardar-documento-id", "result.value={documentId:(''+(variables.id||''))};", "Copia variables.id (resposta de Criar documento) para documentId.");
            SeedJsFunction("clicksign-guardar-signatario1-id", "result.value={signer1Id:(''+(variables.id||''))};", "Copia variables.id (resposta de Criar signatário 1) para signer1Id.");
            SeedJsFunction("clicksign-guardar-signatario2-id", "result.value={signer2Id:(''+(variables.id||''))};", "Copia variables.id (resposta de Criar signatário 2) para signer2Id.");
            SeedJsFunction("clicksign-guardar-signatario3-id", "result.value={signer3Id:(''+(variables.id||''))};", "Copia variables.id (resposta de Criar signatário 3) para signer3Id.");

            SeedJsFunction("clicksign-normalizar-webhook",
                """
                function s(v){return v==null?'':(''+v);}
                function ob(v){if(v==null){return null;}if(typeof v.ToString==='function'){try{return JSON.parse(v.ToString());}catch(e){return null;}}return v;}
                var evt=ob(payload.event);
                var name=evt?s(evt.name):'';
                var data=evt?ob(evt.data):null;
                var doc=data?ob(data.document):null;
                var signer=data?ob(data.signer):null;
                var refusal=data?ob(data.refusal):null;
                var eventType=name;
                var metadata='';
                if(name==='sign'){eventType='signer.signed';}
                else if(name==='refusal'){eventType='rejected';metadata=refusal?s(refusal.reason):'';}
                else if(name==='close'||name==='auto_close'||name==='document_closed'){eventType='completed';}
                else if(name==='cancel'){eventType='cancelled';}
                else if(name==='upload'){eventType='created';}
                result.value={
                  providerDocumentId:doc?(s(doc.key)||s(doc.id)):'',
                  acEventType:eventType,
                  signerEmail:signer?s(signer.email):'',
                  providerSignerId:signer?(s(signer.key)||s(signer.id)):'',
                  webhookMetadata:metadata
                };
                """,
                "Traduz o webhook ClickSign (event.name/event.data.document/event.data.signer) para o callback do Mainstay: sign -> signer.signed, refusal -> rejected (com o motivo), close/auto_close/document_closed -> completed, cancel -> cancelled.");

            // --- ApiCalls: fluxo de envio (varias chamadas sequenciais, API v3 nao cria tudo numa chamada) ---
            string authHeaders = """{"Authorization":"{{api_token}}","Content-type":"application/vnd.api+json","Accept":"application/vnd.api+json"}""";

            SeedApiCall("ClickSign - Criar envelope", HttpMethodType.Post, "{{base_url}}/envelopes",
                authHeaders,
                """{"data":{"type":"envelopes","attributes":{"name":{{title | json}},"locale":"pt-BR","auto_close":true,"default_message":{{message | json}}}}}""");

            SeedApiCall("ClickSign - Criar documento", HttpMethodType.Post, "{{base_url}}/envelopes/{{envelopeId}}/documents",
                authHeaders,
                """{"data":{"type":"documents","attributes":{"filename":{{documentName | json}},"content_base64":{{documentBase64 | json}}}}}""");

            SeedApiCall("ClickSign - Criar signatário 1", HttpMethodType.Post, "{{base_url}}/envelopes/{{envelopeId}}/signers", authHeaders, "{{signer1Body}}");
            SeedApiCall("ClickSign - Criar signatário 2", HttpMethodType.Post, "{{base_url}}/envelopes/{{envelopeId}}/signers", authHeaders, "{{signer2Body}}");
            SeedApiCall("ClickSign - Criar signatário 3", HttpMethodType.Post, "{{base_url}}/envelopes/{{envelopeId}}/signers", authHeaders, "{{signer3Body}}");

            SeedApiCall("ClickSign - Requisito qualificação signatário 1", HttpMethodType.Post, "{{base_url}}/envelopes/{{envelopeId}}/requirements", authHeaders,
                """{"data":{"type":"requirements","attributes":{"action":"agree","role":"sign"},"relationships":{"document":{"data":{"type":"documents","id":{{documentId | json}}}},"signer":{"data":{"type":"signers","id":{{signer1Id | json}}}}}}}""");
            SeedApiCall("ClickSign - Requisito autenticação signatário 1", HttpMethodType.Post, "{{base_url}}/envelopes/{{envelopeId}}/requirements", authHeaders,
                """{"data":{"type":"requirements","attributes":{"action":"provide_evidence","auth":"email"},"relationships":{"document":{"data":{"type":"documents","id":{{documentId | json}}}},"signer":{"data":{"type":"signers","id":{{signer1Id | json}}}}}}}""");

            SeedApiCall("ClickSign - Requisito qualificação signatário 2", HttpMethodType.Post, "{{base_url}}/envelopes/{{envelopeId}}/requirements", authHeaders,
                """{"data":{"type":"requirements","attributes":{"action":"agree","role":"sign"},"relationships":{"document":{"data":{"type":"documents","id":{{documentId | json}}}},"signer":{"data":{"type":"signers","id":{{signer2Id | json}}}}}}}""");
            SeedApiCall("ClickSign - Requisito autenticação signatário 2", HttpMethodType.Post, "{{base_url}}/envelopes/{{envelopeId}}/requirements", authHeaders,
                """{"data":{"type":"requirements","attributes":{"action":"provide_evidence","auth":"email"},"relationships":{"document":{"data":{"type":"documents","id":{{documentId | json}}}},"signer":{"data":{"type":"signers","id":{{signer2Id | json}}}}}}}""");

            SeedApiCall("ClickSign - Requisito qualificação signatário 3", HttpMethodType.Post, "{{base_url}}/envelopes/{{envelopeId}}/requirements", authHeaders,
                """{"data":{"type":"requirements","attributes":{"action":"agree","role":"sign"},"relationships":{"document":{"data":{"type":"documents","id":{{documentId | json}}}},"signer":{"data":{"type":"signers","id":{{signer3Id | json}}}}}}}""");
            SeedApiCall("ClickSign - Requisito autenticação signatário 3", HttpMethodType.Post, "{{base_url}}/envelopes/{{envelopeId}}/requirements", authHeaders,
                """{"data":{"type":"requirements","attributes":{"action":"provide_evidence","auth":"email"},"relationships":{"document":{"data":{"type":"documents","id":{{documentId | json}}}},"signer":{"data":{"type":"signers","id":{{signer3Id | json}}}}}}}""");

            SeedApiCall("ClickSign - Ativar envelope", HttpMethodType.Patch, "{{base_url}}/envelopes/{{envelopeId}}", authHeaders,
                """{"data":{"id":{{envelopeId | json}},"type":"envelopes","attributes":{"status":"running"}}}""");

            SeedApiCall("ClickSign - Notificar signatários", HttpMethodType.Post, "{{base_url}}/envelopes/{{envelopeId}}/notifications", authHeaders,
                """{"data":{"type":"notifications","attributes":{}}}""");

            SeedApiCall("ClickSign - Callback created", HttpMethodType.Post, "{{callbackBaseUrl}}/api/campaigndocuments/provider-callback/{{callbackToken}}",
                """{"x-webhook-secret":"{{CallbackSecret}}","Content-Type":"application/json"}""",
                """{"provider":"clicksign","eventType":"created","campaignDocumentId":{{campaignDocumentId}},"providerDocumentId":{{documentId | json}}}""");

            SeedApiCall("ClickSign - Callback sent signatario 1", HttpMethodType.Post, "{{callbackBaseUrl}}/api/campaigndocuments/provider-callback/{{callbackToken}}",
                """{"x-webhook-secret":"{{CallbackSecret}}","Content-Type":"application/json"}""",
                """{"provider":"clicksign","eventType":"sent","campaignDocumentId":{{campaignDocumentId}},"providerDocumentId":{{documentId | json}},"signerEmail":{{signer1Email | json}},"providerSignerId":{{signer1Id | json}}}""");

            SeedApiCall("ClickSign - Callback sent signatario 2", HttpMethodType.Post, "{{callbackBaseUrl}}/api/campaigndocuments/provider-callback/{{callbackToken}}",
                """{"x-webhook-secret":"{{CallbackSecret}}","Content-Type":"application/json"}""",
                """{"provider":"clicksign","eventType":"sent","campaignDocumentId":{{campaignDocumentId}},"providerDocumentId":{{documentId | json}},"signerEmail":{{signer2Email | json}},"providerSignerId":{{signer2Id | json}}}""");

            SeedApiCall("ClickSign - Callback sent signatario 3", HttpMethodType.Post, "{{callbackBaseUrl}}/api/campaigndocuments/provider-callback/{{callbackToken}}",
                """{"x-webhook-secret":"{{CallbackSecret}}","Content-Type":"application/json"}""",
                """{"provider":"clicksign","eventType":"sent","campaignDocumentId":{{campaignDocumentId}},"providerDocumentId":{{documentId | json}},"signerEmail":{{signer3Email | json}},"providerSignerId":{{signer3Id | json}}}""");

            SeedApiCall("ClickSign - Callback falha", HttpMethodType.Post, "{{callbackBaseUrl}}/api/campaigndocuments/provider-callback/{{callbackToken}}",
                """{"x-webhook-secret":"{{CallbackSecret}}","Content-Type":"application/json"}""",
                """{"provider":"clicksign","eventType":"failed","campaignDocumentId":{{campaignDocumentId}},"metadata":{{errorMessage | json}}}""");

            SeedApiCall("ClickSign - Callback webhook", HttpMethodType.Post, "{{callbackBaseUrl}}/api/campaigndocuments/provider-callback/{{callbackToken}}",
                """{"x-webhook-secret":"{{CallbackSecret}}","Content-Type":"application/json"}""",
                """{"provider":"clicksign","eventType":{{acEventType | json}},"providerDocumentId":{{providerDocumentId | json}},"signerEmail":{{signerEmail | json}},"providerSignerId":{{providerSignerId | json}},"metadata":{{webhookMetadata | json}}}""");

            // --- Pipeline: Enviar para assinatura ---
            SeedPipeline("clicksign", "clicksign-enviar-assinatura", "Enviar para assinatura", "Cria envelope, documento e signatários (até 3) na ClickSign, ativa o envelope e dispara a notificação nativa da ClickSign (e-mail) para cada signatário.", isDefault: false, isTestPipeline: false, contractIdentifier: "assinatura.enviar");

            SeedStep("clicksign-enviar-assinatura", 1, "Normalizar envio", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "clicksign-normalizar-envio");
            SeedStep("clicksign-enviar-assinatura", 2, "Criar envelope", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "ClickSign - Criar envelope");
            SeedStep("clicksign-enviar-assinatura", 3, "Guardar envelopeId", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "clicksign-guardar-envelope-id");
            SeedStep("clicksign-enviar-assinatura", 4, "Criar documento", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "ClickSign - Criar documento");
            SeedStep("clicksign-enviar-assinatura", 5, "Guardar documentId", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "clicksign-guardar-documento-id");

            SeedStep("clicksign-enviar-assinatura", 6, "Criar signatário 1", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "ClickSign - Criar signatário 1");
            SeedStep("clicksign-enviar-assinatura", 7, "Guardar signatário 1 id", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "clicksign-guardar-signatario1-id");
            SeedStep("clicksign-enviar-assinatura", 8, "Requisito qualificação signatário 1", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "ClickSign - Requisito qualificação signatário 1");
            SeedStep("clicksign-enviar-assinatura", 9, "Requisito autenticação signatário 1", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "ClickSign - Requisito autenticação signatário 1");

            SeedStep("clicksign-enviar-assinatura", 10, "Criar signatário 2", PipelineStepType.HttpRequest, ErrorAction.Continue, apiCall: "ClickSign - Criar signatário 2");
            SeedStep("clicksign-enviar-assinatura", 11, "Guardar signatário 2 id", PipelineStepType.JavaScriptFunction, ErrorAction.Continue, jsFunction: "clicksign-guardar-signatario2-id");
            SeedStep("clicksign-enviar-assinatura", 12, "Requisito qualificação signatário 2", PipelineStepType.HttpRequest, ErrorAction.Continue, apiCall: "ClickSign - Requisito qualificação signatário 2");
            SeedStep("clicksign-enviar-assinatura", 13, "Requisito autenticação signatário 2", PipelineStepType.HttpRequest, ErrorAction.Continue, apiCall: "ClickSign - Requisito autenticação signatário 2");

            SeedStep("clicksign-enviar-assinatura", 14, "Criar signatário 3", PipelineStepType.HttpRequest, ErrorAction.Continue, apiCall: "ClickSign - Criar signatário 3");
            SeedStep("clicksign-enviar-assinatura", 15, "Guardar signatário 3 id", PipelineStepType.JavaScriptFunction, ErrorAction.Continue, jsFunction: "clicksign-guardar-signatario3-id");
            SeedStep("clicksign-enviar-assinatura", 16, "Requisito qualificação signatário 3", PipelineStepType.HttpRequest, ErrorAction.Continue, apiCall: "ClickSign - Requisito qualificação signatário 3");
            SeedStep("clicksign-enviar-assinatura", 17, "Requisito autenticação signatário 3", PipelineStepType.HttpRequest, ErrorAction.Continue, apiCall: "ClickSign - Requisito autenticação signatário 3");

            SeedStep("clicksign-enviar-assinatura", 18, "Ativar envelope", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "ClickSign - Ativar envelope");
            SeedStep("clicksign-enviar-assinatura", 19, "Notificar signatários", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "ClickSign - Notificar signatários", ignoreOnResponse: true);

            SeedStep("clicksign-enviar-assinatura", 20, "Callback created", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "ClickSign - Callback created", ignoreOnResponse: true);
            SeedStep("clicksign-enviar-assinatura", 21, "Callback sent signatario 1", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "ClickSign - Callback sent signatario 1", ignoreOnResponse: true);
            SeedStep("clicksign-enviar-assinatura", 22, "Callback sent signatario 2", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "ClickSign - Callback sent signatario 2", ignoreOnResponse: true);
            SeedStep("clicksign-enviar-assinatura", 23, "Callback sent signatario 3", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "ClickSign - Callback sent signatario 3", ignoreOnResponse: true);
            SeedStep("clicksign-enviar-assinatura", 24, "Callback falha", PipelineStepType.HttpRequest, ErrorAction.Continue, apiCall: "ClickSign - Callback falha", ignoreOnResponse: true);

            // Nome do pipeline segue a convencao {integration}-webhook do receptor de webhooks do IP.
            SeedPipeline("clicksign", "clicksign-webhook", "Webhook ClickSign", "Recebe os eventos da ClickSign (sign, refusal, close), normaliza e repassa ao callback do Mainstay.", isDefault: false, isTestPipeline: false, contractIdentifier: null);
            SeedStep("clicksign-webhook", 1, "Normalizar webhook", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "clicksign-normalizar-webhook");
            SeedStep("clicksign-webhook", 2, "Callback webhook", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "ClickSign - Callback webhook", ignoreOnResponse: true);

            // runOnError no callback de falha (o helper base SeedStep nao seta a coluna runonerror).
            Execute.Sql("""
                UPDATE pipelinestep SET runonerror = true, updatedat = now()
                WHERE apicallid = (SELECT id FROM apicall WHERE name = 'ClickSign - Callback falha')
                  AND pipelineid IN (SELECT id FROM pipeline WHERE integrationid = (SELECT id FROM integration WHERE identifier = 'clicksign'));
                """);

            // Steps do bloco signatário 2/3 (criação, guardar id, 2 requisitos = 4 steps cada) só rodam
            // quando aquele signatário existe (o helper base SeedStep nao seta runcondition).
            Execute.Sql("""
                UPDATE pipelinestep SET runcondition = '!!variables.hasSigner2', updatedat = now()
                WHERE pipelineid = (SELECT id FROM pipeline WHERE identifier = 'clicksign-enviar-assinatura') AND "order" IN (10, 11, 12, 13);
                """);
            Execute.Sql("""
                UPDATE pipelinestep SET runcondition = '!!variables.hasSigner3', updatedat = now()
                WHERE pipelineid = (SELECT id FROM pipeline WHERE identifier = 'clicksign-enviar-assinatura') AND "order" IN (14, 15, 16, 17);
                """);

            // Callbacks sent 2 e 3 tambem so rodam quando o signatario existe.
            Execute.Sql("""
                UPDATE pipelinestep SET runcondition = '!!variables.hasSigner2', updatedat = now()
                WHERE pipelineid = (SELECT id FROM pipeline WHERE identifier = 'clicksign-enviar-assinatura') AND "order" = 22;
                """);
            Execute.Sql("""
                UPDATE pipelinestep SET runcondition = '!!variables.hasSigner3', updatedat = now()
                WHERE pipelineid = (SELECT id FROM pipeline WHERE identifier = 'clicksign-enviar-assinatura') AND "order" = 23;
                """);
        }

        public override void Down()
        {
            // Catalogo/integracao de convergencia; Down nao reverte (connectors e execucoes referenciam estes registros).
        }
    }
}
