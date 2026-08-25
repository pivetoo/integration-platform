using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations.ContasAReceber.Asaas
{
    // Testado ao vivo (execution 50, pipeline asaas-webhook): a etapa "Callback webhook" devolveu 404
    // porque {{callbackToken}} nunca resolve num pipeline disparado por webhook (so vem preenchido
    // quando o AgencyCampaign chama um contrato e ecoa esse campo no payload) - mesma causa raiz ja
    // corrigida para o ClickSign (Migration_202608240007_CorrigeWebhookClickSign). Confirmado com uma
    // execucao real da Asaas (asaas-criar-cobranca) que o formato esperado e "{tenantId}~{financialEntryId}".
    // Fix: a propria normalizacao do webhook monta callbackToken = variables.tenantId + '~' + financialEntryId.
    //
    // Corrige tambem um segundo bug achado no mesmo teste: amountPaid saia como numero JS (Number(...)),
    // e o interpolador do template converteu para string usando separador decimal de virgula
    // ("amountPaid":8,2 - JSON invalido). Fix: formata como string na propria funcao JS (s(pay.value),
    // sempre com ponto) e o body so faz substituicao textual, sem reformatar.
    [Migration(202608250004)]
    public sealed class Migration_202608250004_CorrigeCallbackWebhookAsaas : Migration
    {
        public override void Up()
        {
            Execute.Sql("""
                UPDATE javascriptfunction SET
                    code = $CODE$
                function s(v){return v==null?'':(''+v);}
                function ob(v){if(v==null){return null;}if(typeof v.ToString==='function'){try{return JSON.parse(v.ToString());}catch(e){return null;}}return v;}
                var headers=ob(payload.webhookHeaders)||{};
                var receivedToken=s(headers['asaas-access-token']);
                if(!secretEquals('webhook_auth_token',receivedToken)){throw new Error('Token de webhook Asaas invalido ou ausente.');}
                var evt=s(payload.event);
                var pay=ob(payload.payment)||{};
                var eventType='';
                if(evt==='PAYMENT_RECEIVED'||evt==='PAYMENT_CONFIRMED'){eventType='paid';}
                else if(evt==='PAYMENT_DELETED'){eventType='cancelled';}
                else {eventType='updated';}
                var financialEntryId=s(pay.externalReference);
                result.value={
                  acEventType:eventType,
                  chargeId:s(pay.id),
                  financialEntryId:financialEntryId,
                  callbackToken:s(variables.tenantId)+'~'+financialEntryId,
                  amountPaid:s(pay.value),
                  paidAt:(pay.clientPaymentDate||pay.paymentDate||null)
                };
                $CODE$,
                    updatedat = now()
                WHERE name = 'asaas-normalizar-webhook-pagamento';
                """);
        }

        public override void Down()
        {
        }
    }
}
