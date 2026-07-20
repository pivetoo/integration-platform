using FluentMigrator;
using IntegrationPlatform.Domain.ValueObjects;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations.AssinaturaDigital.D4Sign
{
    // Integracao D4Sign de assinatura digital (API v1, autenticacao por tokenAPI + cryptKey na query string).
    // Decisoes de desenho:
    // - Fluxo em 4 chamadas (limitacao da API): uploadbinary (PDF base64 no cofre) -> createlist
    //   (signatarios) -> webhooks (postback por documento) -> sendtosigner (dispara os e-mails).
    // - O postback da D4Sign e form-data e NAO ecoa identificador nosso; a correlacao vai no path da
    //   URL registrada por documento (/api/webhooks/{tenant}/d4sign/{callbackToken}) e chega ao
    //   pipeline como payload.webhookContext. type_post: 1=finalizado, 2=falha de e-mail,
    //   3=cancelado, 4=assinado por um signatario.
    // - providerDocumentId e capturado logo apos o upload (respostas seguintes nao carregam uuid).
    // - Sem sign_url por signatario (a D4Sign nao devolve link no fluxo básico): o botao Assinar do
    //   portal do creator nao aparece; o signatario assina pelo e-mail da D4Sign.
    // - Rate limit default da D4Sign e 10 req/h (cada envio consome 4): orientar o cliente a pedir
    //   aumento ao suporte D4Sign; instrucao na descricao do tokenAPI.
    // Depende do catalogo assinatura-digital do SeedZapSign (versao menor).
    [Migration(202607200003)]
    public sealed class Migration_202607200003_SeedD4Sign : IntegrationSeedMigration
    {
        public override void Up()
        {
            SeedIntegration("d4sign", "D4Sign", "Assinatura digital de documentos e contratos pela D4Sign. Envie o PDF, os signatários assinam pelo link recebido por e-mail e o resultado (assinado ou recusado) volta automaticamente para o Mainstay.", "assinatura-digital", "https://logos.hunter.io/d4sign.com.br", supportsWebhook: true);

            SeedAttribute("d4sign", "tokenAPI", "Token de API", FieldType.Text, required: true, order: 1, group: "Autenticação", sensitive: true, description: "Token de API da conta D4Sign (Configurações > API). Atenção: o limite padrão da D4Sign é de 10 requisições por hora e cada envio consome 4 — peça aumento de limite ao suporte da D4Sign antes de usar em produção.");
            SeedAttribute("d4sign", "cryptKey", "Crypt Key", FieldType.Text, required: false, order: 2, group: "Autenticação", sensitive: true, description: "Chave de criptografia da conta D4Sign. Preencha apenas se estiver habilitada na sua conta.");
            SeedAttribute("d4sign", "uuid_safe", "Cofre (UUID)", FieldType.Text, required: true, order: 3, group: "Documentos", description: "UUID do cofre da D4Sign onde os documentos serão criados (painel D4Sign > Cofres).");
            SeedAttribute("d4sign", "base_url", "Base da API", FieldType.Text, required: true, order: 4, group: "Endpoints", hidden: true, description: "Base da API v1. Produção por padrão; sandbox = https://sandbox.d4sign.com.br/api/v1.", defaultValue: "https://secure.d4sign.com.br/api/v1");
            SeedAttribute("d4sign", "callbackBaseUrl", "URL base do Mainstay (callback)", FieldType.Text, required: false, order: 5, group: "Webhook", hidden: true, description: "Base do AgencyCampaign que recebe os callbacks.", defaultValue: "https://agencias.mainstay.com.br");
            SeedAttribute("d4sign", "webhookBaseUrl", "URL base da Plataforma (postback)", FieldType.Text, required: false, order: 6, group: "Webhook", hidden: true, description: "Base da IntegrationPlatform registrada como postback por documento na D4Sign.", defaultValue: "https://integrations.mainstay.com.br");

            BindContract("d4sign", "assinatura.enviar");

            // --- JavaScript functions ---
            SeedJsFunction("d4sign-normalizar-envio",
                """
                function s(v){return v==null?'':(''+v);}
                var signers=payload.signers||[];
                var list=[];
                for(var i=0;i<signers.length;i++){
                  var it=signers[i];
                  list.push({email:s(it.email),act:'1',foreign:'0',certificadoicpbr:'0',assinatura_presencial:'0'});
                }
                result.value={
                  d4signUploadBody:JSON.stringify({base64_binary_file:s(payload.documentBase64),mime_type:'application/pdf',name:(s(payload.title)||'Documento')}),
                  d4signSignersBody:JSON.stringify({signers:list}),
                  d4signSendBody:JSON.stringify({skip_email:'0',workflow:'0',message:s(payload.message)}),
                  primeiroSignerEmail:list.length?list[0].email:''
                };
                """,
                "Monta os bodies D4Sign: uploadbinary (PDF base64), createlist (signatários com act=1/assinar) e sendtosigner (workflow paralelo, com e-mail).");

            SeedJsFunction("d4sign-normalizar-resposta-envio",
                """
                function s(v){return v==null?'':(''+v);}
                result.value={providerDocumentId:s(variables.uuid)};
                """,
                "Captura o uuid do documento retornado pelo uploadbinary como providerDocumentId (as respostas seguintes não carregam o uuid).");

            SeedJsFunction("d4sign-normalizar-webhook",
                """
                function s(v){return v==null?'':(''+v);}
                var tp=s(payload.type_post);
                var eventType='d4sign:'+tp;
                if(tp==='1'){eventType='document.completed';}
                else if(tp==='4'){eventType='signer.signed';}
                else if(tp==='3'){eventType='cancelled';}
                else if(tp==='2'){eventType='email_bounce';}
                result.value={
                  callbackToken:s(payload.webhookContext),
                  providerDocumentId:s(payload.uuid),
                  acEventType:eventType,
                  signerEmail:s(payload.email),
                  webhookMetadata:s(payload.message)
                };
                """,
                "Traduz o postback D4Sign (form-data) para o callback do Mainstay: type_post 1 -> document.completed, 4 -> signer.signed, 3 -> cancelled, 2 -> email_bounce; correlação via payload.webhookContext (callbackToken no path).");

            // --- ApiCalls ---
            SeedApiCall("D4Sign - Upload documento", HttpMethodType.Post, "{{base_url}}/documents/{{uuid_safe}}/uploadbinary?tokenAPI={{tokenAPI}}&cryptKey={{cryptKey}}",
                """{"Content-Type":"application/json"}""",
                "{{d4signUploadBody}}");

            SeedApiCall("D4Sign - Cadastrar signatarios", HttpMethodType.Post, "{{base_url}}/documents/{{providerDocumentId}}/createlist?tokenAPI={{tokenAPI}}&cryptKey={{cryptKey}}",
                """{"Content-Type":"application/json"}""",
                "{{d4signSignersBody}}");

            SeedApiCall("D4Sign - Registrar postback", HttpMethodType.Post, "{{base_url}}/documents/{{providerDocumentId}}/webhooks?tokenAPI={{tokenAPI}}&cryptKey={{cryptKey}}",
                """{"Content-Type":"application/json"}""",
                """{"url":"{{webhookBaseUrl}}/api/webhooks/{{TenantId}}/d4sign/{{callbackToken}}"}""");

            SeedApiCall("D4Sign - Enviar para assinatura", HttpMethodType.Post, "{{base_url}}/documents/{{providerDocumentId}}/sendtosigner?tokenAPI={{tokenAPI}}&cryptKey={{cryptKey}}",
                """{"Content-Type":"application/json"}""",
                "{{d4signSendBody}}");

            SeedApiCall("D4Sign - Callback created", HttpMethodType.Post, "{{callbackBaseUrl}}/api/campaigndocuments/provider-callback/{{callbackToken}}",
                """{"x-webhook-secret":"{{CallbackSecret}}","Content-Type":"application/json"}""",
                """{"provider":"d4sign","eventType":"created","campaignDocumentId":{{campaignDocumentId}},"providerDocumentId":{{providerDocumentId | json}}}""");

            SeedApiCall("D4Sign - Callback sent", HttpMethodType.Post, "{{callbackBaseUrl}}/api/campaigndocuments/provider-callback/{{callbackToken}}",
                """{"x-webhook-secret":"{{CallbackSecret}}","Content-Type":"application/json"}""",
                """{"provider":"d4sign","eventType":"sent","campaignDocumentId":{{campaignDocumentId}},"providerDocumentId":{{providerDocumentId | json}},"signerEmail":{{primeiroSignerEmail | json}}}""");

            SeedApiCall("D4Sign - Callback falha", HttpMethodType.Post, "{{callbackBaseUrl}}/api/campaigndocuments/provider-callback/{{callbackToken}}",
                """{"x-webhook-secret":"{{CallbackSecret}}","Content-Type":"application/json"}""",
                """{"provider":"d4sign","eventType":"failed","campaignDocumentId":{{campaignDocumentId}},"metadata":{{errorMessage | json}}}""");

            SeedApiCall("D4Sign - Callback webhook", HttpMethodType.Post, "{{callbackBaseUrl}}/api/campaigndocuments/provider-callback/{{callbackToken}}",
                """{"x-webhook-secret":"{{CallbackSecret}}","Content-Type":"application/json"}""",
                """{"provider":"d4sign","eventType":{{acEventType | json}},"providerDocumentId":{{providerDocumentId | json}},"signerEmail":{{signerEmail | json}},"metadata":{{webhookMetadata | json}}}""");

            // --- Pipelines + Steps ---
            SeedPipeline("d4sign", "d4sign-enviar-assinatura", "Enviar para assinatura", "Sobe o PDF no cofre, cadastra os signatários, registra o postback por documento e dispara o envio; correlaciona via callback created.", isDefault: true, isTestPipeline: false, contractIdentifier: "assinatura.enviar");
            SeedStep("d4sign-enviar-assinatura", 1, "Normalizar envio", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "d4sign-normalizar-envio");
            SeedStep("d4sign-enviar-assinatura", 2, "Upload documento", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "D4Sign - Upload documento");
            SeedStep("d4sign-enviar-assinatura", 3, "Normalizar resposta", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "d4sign-normalizar-resposta-envio");
            SeedStep("d4sign-enviar-assinatura", 4, "Cadastrar signatarios", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "D4Sign - Cadastrar signatarios");
            SeedStep("d4sign-enviar-assinatura", 5, "Registrar postback", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "D4Sign - Registrar postback");
            SeedStep("d4sign-enviar-assinatura", 6, "Enviar para assinatura", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "D4Sign - Enviar para assinatura");
            SeedStep("d4sign-enviar-assinatura", 7, "Callback created", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "D4Sign - Callback created", ignoreOnResponse: true);
            SeedStep("d4sign-enviar-assinatura", 8, "Callback sent", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "D4Sign - Callback sent", ignoreOnResponse: true);
            SeedStep("d4sign-enviar-assinatura", 9, "Callback falha", PipelineStepType.HttpRequest, ErrorAction.Continue, apiCall: "D4Sign - Callback falha", ignoreOnResponse: true);

            // Nome do pipeline segue a convencao {integration}-webhook do receptor de webhooks do IP.
            SeedPipeline("d4sign", "d4sign-webhook", "Webhook D4Sign", "Recebe os postbacks da D4Sign (form-data), normaliza e repassa ao callback do Mainstay.", isDefault: false, isTestPipeline: false, contractIdentifier: null);
            SeedStep("d4sign-webhook", 1, "Normalizar webhook", PipelineStepType.JavaScriptFunction, ErrorAction.Stop, jsFunction: "d4sign-normalizar-webhook");
            SeedStep("d4sign-webhook", 2, "Callback webhook", PipelineStepType.HttpRequest, ErrorAction.Stop, apiCall: "D4Sign - Callback webhook", ignoreOnResponse: true);

            // runOnError no callback de falha (o helper base SeedStep nao seta a coluna runonerror).
            Execute.Sql("""
                UPDATE pipelinestep SET runonerror = true, updatedat = now()
                WHERE apicallid = (SELECT id FROM apicall WHERE name = 'D4Sign - Callback falha')
                  AND pipelineid IN (SELECT id FROM pipeline WHERE integrationid = (SELECT id FROM integration WHERE identifier = 'd4sign'));
                """);
        }

        public override void Down()
        {
            // Catalogo/integracao de convergencia; Down nao reverte (connectors e execucoes referenciam estes registros).
        }
    }
}
