using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations.AssinaturaDigital.ClickSign
{
    // A Migration_202608240007 fez um SET completo de clicksign-normalizar-envio.code baseado no
    // texto original (pre-202608240006), sobrescrevendo sem querer o fix do Data URI - testado ao
    // vivo (execution 30): voltou o mesmo 400 "content_base64 ... Data URI completo" que a
    // Migration_202608240006 ja tinha corrigido. Reaplica os DOIS fixes juntos desta vez.
    [Migration(202608240008)]
    public sealed class Migration_202608240008_RestauraDataUriClickSign : Migration
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
                var rawBase64=s(payload.documentBase64);
                out.documentBase64=rawBase64.indexOf('data:')===0?rawBase64:('data:application/pdf;base64,'+rawBase64);
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

            Execute.Sql($"""
                UPDATE javascriptfunction SET code = {Txt(normalizarEnvioCode)}, updatedat = now()
                WHERE name = 'clicksign-normalizar-envio';
                """);
        }

        public override void Down()
        {
            // Ajuste de funcao JS; nao ha estado anterior a restaurar.
        }
    }
}
