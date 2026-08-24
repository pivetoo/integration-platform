using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations.AssinaturaDigital
{
    // Mesma classe de bug da Migration_202608240001 (JsonElement do payload nao vira valor JS nativo
    // no Jint), desta vez num objeto aninhado em vez de um array: "payload.signer_who_signed" chega
    // como JsonElement de objeto, que nao expoe ".email"/".token" via acesso de propriedade JS -
    // "who.email" sempre voltava undefined, entao o webhook doc_signed processava com sucesso (200)
    // mas nunca marcava o signatario certo como assinado (signerEmail sempre vazio). Aplica o mesmo
    // contorno (JSON.parse(v.ToString())) usado no helper el() da normalizacao de resposta do envio.
    [Migration(202608240002)]
    public sealed class Migration_202608240002_CorrigeSignerWhoSignedWebhookZapSign : Migration
    {
        public override void Up()
        {
            Execute.Sql("""
                UPDATE javascriptfunction SET
                    code = $$
                function s(v){return v==null?'':(''+v);}
                function ob(v){if(v==null){return null;}if(typeof v.ToString==='function'){try{return JSON.parse(v.ToString());}catch(e){return null;}}return v;}
                var evt=s(payload.event_type);
                var eventType=evt;
                var metadata='';
                if(evt==='doc_signed'){eventType='signer.signed';}
                else if(evt==='doc_refused'){eventType='rejected';metadata=s(payload.rejected_reason);}
                else if(evt==='doc_created'){eventType='created';}
                else if(evt==='doc_deleted'){eventType='cancelled';}
                var who=ob(payload.signer_who_signed);
                result.value={
                  callbackToken:s(payload.external_id),
                  providerDocumentId:s(payload.token),
                  acEventType:eventType,
                  signerEmail:who?s(who.email):'',
                  providerSignerId:who?s(who.token):'',
                  signedDocumentUrl:s(payload.signed_file),
                  webhookMetadata:metadata
                };
                $$,
                    updatedat = now()
                WHERE name = 'zapsign-normalizar-webhook';
                """);
        }

        public override void Down()
        {
            // Correcao de bug; nao ha estado anterior valido a restaurar.
        }
    }
}
