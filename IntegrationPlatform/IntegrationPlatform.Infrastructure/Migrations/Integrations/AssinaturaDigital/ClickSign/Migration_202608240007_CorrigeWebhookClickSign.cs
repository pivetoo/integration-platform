using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations.AssinaturaDigital.ClickSign
{
    // Testado ao vivo (execution 29, pipeline clicksign-webhook): a etapa "Callback webhook" devolveu
    // 404 porque {{callbackToken}} nunca resolvia nesse contexto - o webhook da ClickSign nao carrega o
    // callbackToken em lugar nenhum por conta propria (diferente da ZapSign, que ecoa "external_id").
    // Fix, no padrao "metadata" que a ClickSign documenta para isso (POST .../documents aceita
    // "metadata": JSON usado nos retornos via webhook):
    // 1. clicksign-normalizar-envio passa a montar documentMetadata = {"callbackToken": "..."};
    // 2. "ClickSign - Criar documento" inclui esse metadata no body;
    // 3. clicksign-normalizar-webhook le de volta em payload.document.metadata.callbackToken.
    //
    // Corrige tambem dois bugs de parsing achados comparando o payload REAL capturado (execution 29,
    // evento "upload") com a documentacao oficial (developers.clicksign.com/docs/evento-close,
    // /docs/evento-sign, /docs/evento-refusal):
    // - "document" e IRMAO de "event" na raiz do payload, nao aninhado em event.data.document (o
    //   codigo original nunca achava o documento em nenhum evento, so nao dava pra perceber porque o
    //   pipeline ja falhava antes, no 404 do callbackToken);
    // - o evento refusal carrega o motivo em event.data.refusal.comment, nao ".reason".
    [Migration(202608240007)]
    public sealed class Migration_202608240007_CorrigeWebhookClickSign : Migration
    {
        private static string Txt(string value) => "'" + value.Replace("'", "''") + "'";

        public override void Up()
        {
            const string normalizarEnvioCode = """
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
                out.documentMetadata=JSON.stringify({callbackToken:s(payload.callbackToken)});
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
                """;

            const string normalizarWebhookCode = """
                function s(v){return v==null?'':(''+v);}
                function ob(v){if(v==null){return null;}if(typeof v.ToString==='function'){try{return JSON.parse(v.ToString());}catch(e){return null;}}return v;}
                var evt=ob(payload.event);
                var name=evt?s(evt.name):'';
                var data=evt?ob(evt.data):null;
                var doc=ob(payload.document);
                var signer=data?ob(data.signer):null;
                var refusal=data?ob(data.refusal):null;
                var docMetadata=doc?ob(doc.metadata):null;
                var eventType=name;
                var metadata='';
                if(name==='sign'){eventType='signer.signed';}
                else if(name==='refusal'){eventType='rejected';metadata=refusal?s(refusal.comment):'';}
                else if(name==='close'||name==='auto_close'||name==='document_closed'){eventType='completed';}
                else if(name==='cancel'){eventType='cancelled';}
                else if(name==='upload'){eventType='created';}
                result.value={
                  callbackToken:docMetadata?s(docMetadata.callbackToken):'',
                  providerDocumentId:doc?(s(doc.key)||s(doc.id)):'',
                  acEventType:eventType,
                  signerEmail:signer?s(signer.email):'',
                  providerSignerId:signer?(s(signer.key)||s(signer.id)):'',
                  webhookMetadata:metadata
                };
                """;

            const string criarDocumentoBody = """{"data":{"type":"documents","attributes":{"filename":{{documentName | json}},"content_base64":{{documentBase64 | json}},"metadata":{{documentMetadata}}}}}""";

            Execute.Sql($"""
                UPDATE javascriptfunction SET code = {Txt(normalizarEnvioCode)}, updatedat = now()
                WHERE name = 'clicksign-normalizar-envio';
                """);

            Execute.Sql($"""
                UPDATE javascriptfunction SET code = {Txt(normalizarWebhookCode)}, updatedat = now()
                WHERE name = 'clicksign-normalizar-webhook';
                """);

            Execute.Sql($"""
                UPDATE apicall SET bodytemplate = {Txt(criarDocumentoBody)}, updatedat = now()
                WHERE name = 'ClickSign - Criar documento';
                """);
        }

        public override void Down()
        {
            // Ajuste de funcoes JS e template de body; nao ha estado anterior a restaurar.
        }
    }
}
