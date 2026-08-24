using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations.AssinaturaDigital
{
    // O payload de uma execucao chega como Dictionary<string, object> via
    // JsonSerializer.Deserialize (PipelinePayloadParser), entao qualquer campo array/objeto vira um
    // System.Text.Json.JsonElement boxado - nao um array JS nativo quando exposto ao Jint. Ler
    // "payload.signers" direto funciona para campos escalares (title, documentBase64) porque
    // ''+v aciona JsonElement.ToString(), mas falha silenciosamente para arrays: "signers.length"
    // fica undefined, o for nao roda e o array de signatarios sai vazio - a ZapSign/D4Sign entao
    // recusam com 400 "signers e obrigatorio". A funcao zapsign-normalizar-resposta-envio ja
    // contornava esse mesmo problema (helper el()) para o campo "signers" da RESPOSTA da ZapSign;
    // faltava aplicar o mesmo contorno na normalizacao de ENVIO, em ambas as integracoes.
    [Migration(202608240001)]
    public sealed class Migration_202608240001_CorrigeArraySignersEnvioAssinatura : Migration
    {
        public override void Up()
        {
            Execute.Sql("""
                UPDATE javascriptfunction SET
                    code = $$
                function s(v){return v==null?'':(''+v);}
                function el(v){if(v==null){return [];}if(typeof v.length!=='number'&&typeof v.ToString==='function'){try{return JSON.parse(v.ToString());}catch(e){return [];}}return v;}
                var signers=el(payload.signers);
                var out=[];
                for(var i=0;i<signers.length;i++){
                  var it=signers[i];
                  out.push({name:s(it.name),email:s(it.email),auth_mode:'assinaturaTela',send_automatic_email:true,custom_message:s(payload.message)});
                }
                var body={name:(s(payload.title)||'Documento'),base64_pdf:s(payload.documentBase64),external_id:s(payload.callbackToken),lang:'pt-br',signers:out};
                result.value={zapsignDocBody:JSON.stringify(body)};
                $$,
                    updatedat = now()
                WHERE name = 'zapsign-normalizar-envio';
                """);

            Execute.Sql("""
                UPDATE javascriptfunction SET
                    code = $$
                function s(v){return v==null?'':(''+v);}
                function el(v){if(v==null){return [];}if(typeof v.length!=='number'&&typeof v.ToString==='function'){try{return JSON.parse(v.ToString());}catch(e){return [];}}return v;}
                var signers=el(payload.signers);
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
                $$,
                    updatedat = now()
                WHERE name = 'd4sign-normalizar-envio';
                """);
        }

        public override void Down()
        {
            // Correcao de bug; nao ha estado anterior valido a restaurar.
        }
    }
}
