using FluentMigrator;
using IntegrationPlatform.Domain.ValueObjects;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations.AssinaturaDigital.ZapSign
{
    // Integracao ZapSign de assinatura digital (API v1, autenticacao Bearer token, sem mTLS).
    // Decisoes de desenho:
    // - Envio em 1 chamada: POST /api/v1/docs/ cria documento (base64_pdf) + signatarios juntos e ja
    //   devolve o sign_url de cada um. external_id = callbackToken do AgencyCampaign, ecoado pelos
    //   webhooks para rotear o retorno sem estado no IP.
    // - send_automatic_email: true por signatario: a ZapSign notifica os signatarios por e-mail
    //   (decisao de produto); o sign_url tambem volta ao Mainstay via callback "sent" por signatario
    //   (ate 3, steps condicionais via runcondition) para o botao Assinar do portal do creator.
    // - Retorno de assinatura via webhook da ZapSign no receptor padrao do IP
    //   (POST /api/webhooks/{tenantId}/zapsign -> pipeline "zapsign-webhook"): doc_signed dispara a
    //   cada signatario (status "signed" so quando todos assinaram, com signed_file) e vira
    //   "signer.signed"; doc_refused vira "rejected".
    // - Setup por tenant: criar o token de API na ZapSign e cadastrar la o webhook (eventos doc_signed
    //   e doc_refused) apontando para https://integrations.mainstay.com.br/api/webhooks/{tenantId}/zapsign.
    [Migration(202607200001)]
    public sealed class Migration_202607200001_SeedZapSign : IntegrationSeedMigration
    {
        public override void Up()
        {
            // --- Catalogo Assinatura Digital ---
            SeedCategory("assinatura-digital", "Assinatura Digital", "Provedores de assinatura eletrônica de documentos: envio para assinatura e retorno de eventos (assinado, recusado).");

            SeedContract(
                identifier: "assinatura.enviar",
                name: "Enviar para assinatura",
                description: "Cria o envelope no provedor com o PDF e os signatários e dispara o fluxo de assinatura.",
                categoryIdentifier: "assinatura-digital",
                inputSchema: """
                {
                  "campaignDocumentId": "number (obrigatorio)",
                  "title": "string (obrigatorio)",
                  "documentBase64": "string PDF base64 (obrigatorio)",
                  "documentName": "string",
                  "mimeType": "application/pdf",
                  "message": "string?",
                  "callbackToken": "string (obrigatorio)",
                  "signers": "[{ role, name, email, documentNumber }] (obrigatorio)"
                }
                """,
                outputSchema: """
                { "assincrono": "callbacks 'created' (providerDocumentId) e 'sent' por signatario (signerEmail, providerSignerId, signingUrl); assinatura/recusa chegam depois via webhook (signer.signed, rejected)" }
                """,
                hasCallback: false,
                callbackSchema: null);

            // --- Integracao ZapSign ---
            SeedIntegration("zapsign", "ZapSign", "Assinatura digital via ZapSign (API v1): envio de PDF com signatários, notificação por e-mail pela ZapSign e retorno por webhook. Cadastre na ZapSign o webhook (eventos doc_signed e doc_refused) apontando para https://integrations.mainstay.com.br/api/webhooks/{tenantId}/zapsign.", "assinatura-digital", "https://logos.hunter.io/zapsign.com.br", supportsWebhook: true);

            SeedAttribute("zapsign", "api_token", "Token de API", FieldType.Text, required: true, order: 1, group: "Autenticação", sensitive: true, description: "Token de API da ZapSign (header Authorization: Bearer), obtido em Configurações > Integrações da conta ZapSign.");
            SeedAttribute("zapsign", "base_url", "Base da API", FieldType.Text, required: true, order: 2, group: "Endpoints", hidden: true, description: "Base da API v1. Produção por padrão; sandbox = https://sandbox.api.zapsign.com.br.", defaultValue: "https://api.zapsign.com.br");
            SeedAttribute("zapsign", "callbackBaseUrl", "URL base do Mainstay (callback)", FieldType.Text, required: false, order: 3, group: "Webhook", hidden: true, description: "Base do AgencyCampaign que recebe os callbacks.", defaultValue: "https://agencias.mainstay.com.br");

            BindContract("zapsign", "assinatura.enviar");

            // --- JavaScript functions ---
            SeedJsFunction("zapsign-normalizar-envio",
                """
                function s(v){return v==null?'':(''+v);}
                var signers=payload.signers||[];
                var out=[];
                for(var i=0;i<signers.length;i++){
                  var it=signers[i];
                  out.push({name:s(it.name),email:s(it.email),auth_mode:'assinaturaTela',send_automatic_email:true,custom_message:s(payload.message)});
                }
                var body={name:(s(payload.title)||'Documento'),base64_pdf:s(payload.documentBase64),external_id:s(payload.callbackToken),lang:'pt-br',signers:out};
                result.value={zapsignDocBody:JSON.stringify(body)};
                """,
                "Monta o body do POST /docs/ ZapSign: PDF base64, external_id = callbackToken (roteia o retorno dos webhooks) e signatários com envio automático de e-mail.");

            SeedJsFunction("zapsign-normalizar-resposta-envio",
                """
                function s(v){return v==null?'':(''+v);}
                function el(v){if(v==null){return [];}if(typeof v.length!=='number'&&typeof v.ToString==='function'){try{return JSON.parse(v.ToString());}catch(e){return [];}}return v;}
                var signers=el(variables.signers);
                var out={providerDocumentId:s(variables.token)};
                for(var i=0;i<3;i++){
                  var it=signers[i]||null;
                  out['signer'+(i+1)+'Email']=it?s(it.email):'';
                  out['signer'+(i+1)+'Id']=it?s(it.token):'';
                  out['signer'+(i+1)+'Url']=it?s(it.sign_url):'';
                }
                result.value=out;
                """,
                "Consolida a criação ZapSign: providerDocumentId (token do doc) e, por signatário (até 3), e-mail, token e sign_url para os callbacks 'sent'.");

            SeedJsFunction("zapsign-normalizar-webhook",
                """
                function s(v){return v==null?'':(''+v);}
                var evt=s(payload.event_type);
                var eventType=evt;
                var metadata='';
                if(evt==='doc_signed'){eventType='signer.signed';}
                else if(evt==='doc_refused'){eventType='rejected';metadata=s(payload.rejected_reason);}
                else if(evt==='doc_created'){eventType='created';}
                else if(evt==='doc_deleted'){eventType='cancelled';}
                var who=payload.signer_who_signed||null;
                result.value={
                  callbackToken:s(payload.external_id),
                  providerDocumentId:s(payload.token),
                  acEventType:eventType,
                  signerEmail:who?s(who.email):'',
                  providerSignerId:who?s(who.token):'',
                  signedDocumentUrl:s(payload.signed_file),
                  webhookMetadata:metadata
                };
                """,
                "Traduz o webhook ZapSign para o callback do Mainstay: doc_signed -> signer.signed (com signer_who_signed e signed_file quando concluído), doc_refused -> rejected (com rejected_reason).");

            // --- ApiCalls ---
            SeedApiCall("ZapSign - Criar documento", HttpMethodType.Post, "{{base_url}}/api/v1/docs/",
                """{"Authorization":"Bearer {{api_token}}","Content-Type":"application/json"}""",
                "{{zapsignDocBody}}");

            SeedApiCall("ZapSign - Callback created", HttpMethodType.Post, "{{callbackBaseUrl}}/api/campaigndocuments/provider-callback/{{callbackToken}}",
                """{"x-webhook-secret":"{{CallbackSecret}}","Content-Type":"application/json"}""",
                """{"provider":"zapsign","eventType":"created","campaignDocumentId":{{campaignDocumentId}},"providerDocumentId":{{providerDocumentId | json}}}""");

            SeedApiCall("ZapSign - Callback sent signatario 1", HttpMethodType.Post, "{{callbackBaseUrl}}/api/campaigndocuments/provider-callback/{{callbackToken}}",
                """{"x-webhook-secret":"{{CallbackSecret}}","Content-Type":"application/json"}""",
                """{"provider":"zapsign","eventType":"sent","campaignDocumentId":{{campaignDocumentId}},"providerDocumentId":{{providerDocumentId | json}},"signerEmail":{{signer1Email | json}},"providerSignerId":{{signer1Id | json}},"signingUrl":{{signer1Url | json}}}""");

            SeedApiCall("ZapSign - Callback sent signatario 2", HttpMethodType.Post, "{{callbackBaseUrl}}/api/campaigndocuments/provider-callback/{{callbackToken}}",
                """{"x-webhook-secret":"{{CallbackSecret}}","Content-Type":"application/json"}""",
                """{"provider":"zapsign","eventType":"sent","campaignDocumentId":{{campaignDocumentId}},"providerDocumentId":{{providerDocumentId | json}},"signerEmail":{{signer2Email | json}},"providerSignerId":{{signer2Id | json}},"signingUrl":{{signer2Url | json}}}""");

            SeedApiCall("ZapSign - Callback sent signatario 3", HttpMethodType.Post, "{{callbackBaseUrl}}/api/campaigndocuments/provider-callback/{{callbackToken}}",
                """{"x-webhook-secret":"{{CallbackSecret}}","Content-Type":"application/json"}""",
                """{"provider":"zapsign","eventType":"sent","campaignDocumentId":{{campaignDocumentId}},"providerDocumentId":{{providerDocumentId | json}},"signerEmail":{{signer3Email | json}},"providerSignerId":{{signer3Id | json}},"signingUrl":{{signer3Url | json}}}""");

            SeedApiCall("ZapSign - Callback falha", HttpMethodType.Post, "{{callbackBaseUrl}}/api/campaigndocuments/provider-callback/{{callbackToken}}",
                """{"x-webhook-secret":"{{CallbackSecret}}","Content-Type":"application/json"}""",
                """{"provider":"zapsign","eventType":"failed","campaignDocumentId":{{campaignDocumentId}},"metadata":{{errorMessage | json}}}""");

            SeedApiCall("ZapSign - Callback webhook", HttpMethodType.Post, "{{callbackBaseUrl}}/api/campaigndocuments/provider-callback/{{callbackToken}}",
                """{"x-webhook-secret":"{{CallbackSecret}}","Content-Type":"application/json"}""",
                """{"provider":"zapsign","eventType":{{acEventType | json}},"providerDocumentId":{{providerDocumentId | json}},"signerEmail":{{signerEmail | json}},"providerSignerId":{{providerSignerId | json}},"signedDocumentUrl":{{signedDocumentUrl | json}},"metadata":{{webhookMetadata | json}}}""");

            // --- Pipelines + Steps ---
            SeedPipeline("zapsign", "zapsign-enviar-assinatura", "Enviar para assinatura", "Cria o documento com signatários na ZapSign (1 chamada), correlaciona via callback created e envia o sign_url de cada signatário via callback sent.", isDefault: true, isTestPipeline: false, contractIdentifier: "assinatura.enviar");
            SeedStep("zapsign-enviar-assinatura", 1, "Normalizar envio", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "zapsign-normalizar-envio");
            SeedStep("zapsign-enviar-assinatura", 2, "Criar documento", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "ZapSign - Criar documento");
            SeedStep("zapsign-enviar-assinatura", 3, "Normalizar resposta", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "zapsign-normalizar-resposta-envio");
            SeedStep("zapsign-enviar-assinatura", 4, "Callback created", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "ZapSign - Callback created", ignoreOnResponse: true);
            SeedStep("zapsign-enviar-assinatura", 5, "Callback sent signatario 1", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "ZapSign - Callback sent signatario 1", ignoreOnResponse: true);
            SeedStep("zapsign-enviar-assinatura", 6, "Callback sent signatario 2", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "ZapSign - Callback sent signatario 2", ignoreOnResponse: true);
            SeedStep("zapsign-enviar-assinatura", 7, "Callback sent signatario 3", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "ZapSign - Callback sent signatario 3", ignoreOnResponse: true);
            SeedStep("zapsign-enviar-assinatura", 8, "Callback falha", PipelineStepType.HttpRequest, ErrorAction.Continue, apiCall: "ZapSign - Callback falha", ignoreOnResponse: true);

            // Nome do pipeline segue a convencao {integration}-webhook do receptor de webhooks do IP.
            SeedPipeline("zapsign", "zapsign-webhook", "Webhook ZapSign", "Recebe os eventos da ZapSign (doc_signed, doc_refused), normaliza e repassa ao callback do Mainstay.", isDefault: false, isTestPipeline: false, contractIdentifier: null);
            SeedStep("zapsign-webhook", 1, "Normalizar webhook", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "zapsign-normalizar-webhook");
            SeedStep("zapsign-webhook", 2, "Callback webhook", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "ZapSign - Callback webhook", ignoreOnResponse: true);

            // runOnError no callback de falha (o helper base SeedStep nao seta a coluna runonerror).
            Execute.Sql("""
                UPDATE pipelinestep SET runonerror = true, updatedat = now()
                WHERE apicallid = (SELECT id FROM apicall WHERE name = 'ZapSign - Callback falha')
                  AND pipelineid IN (SELECT id FROM pipeline WHERE integrationid = (SELECT id FROM integration WHERE identifier = 'zapsign'));
                """);

            // Callbacks sent 2 e 3 so rodam quando o signatario existe (o helper base nao seta runcondition).
            Execute.Sql("""
                UPDATE pipelinestep SET runcondition = '!!variables.signer2Email', updatedat = now()
                WHERE pipelineid = (SELECT id FROM pipeline WHERE identifier = 'zapsign-enviar-assinatura') AND "order" = 6;
                """);
            Execute.Sql("""
                UPDATE pipelinestep SET runcondition = '!!variables.signer3Email', updatedat = now()
                WHERE pipelineid = (SELECT id FROM pipeline WHERE identifier = 'zapsign-enviar-assinatura') AND "order" = 7;
                """);
        }

        public override void Down()
        {
            // Catalogo/integracao de convergencia; Down nao reverte (connectors e execucoes referenciam estes registros).
        }
    }
}
