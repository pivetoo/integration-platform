using FluentMigrator;
using IntegrationPlatform.Infrastructure.Migrations.Integrations;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations.ContasAReceber.Asaas
{
    // O callback do webhook monta o corpo com "paidAt":{{paidAt | json}}, e o AgencyCampaign desserializa esse
    // campo em DateTimeOffset?. Dois defeitos derrubavam o corpo inteiro: o binder falha, o parametro chega nulo
    // e a API responde HTTP 400 "O corpo da requisicao e obrigatorio" — o evento se perde por inteiro.
    //
    // 1. Sem data de pagamento o campo saia vazio e o JSON quebrava. O Asaas manda clientPaymentDate e
    //    paymentDate nulos em todo evento que nao e recebimento: PAYMENT_RECEIVED_IN_CASH_UNDONE e
    //    PAYMENT_DELETED entre eles.
    // 2. A data do proprio evento (dateCreated), usada como alternativa, vem como 'yyyy-MM-dd HH:mm:ss', com
    //    espaco no lugar do T. Nao e ISO 8601 e o System.Text.Json recusa.
    //
    // Bug preexistente, exposto ao testar o desfazimento de recebido em dinheiro ponta a ponta: o titulo
    // continuava pago depois do desfazimento porque o callback nunca chegava. A funcao passa a cair na data do
    // evento quando nao ha data de pagamento e normaliza o separador para T, garantindo corpo sempre valido.
    [Migration(202609120003)]
    public sealed class Migration_202609120003_AsaasWebhookPaidAtNuncaNulo : IntegrationSeedMigration
    {
        public override void Up()
        {
            Execute.Sql("""
                UPDATE javascriptfunction
                SET code = $js$function s(v){return v==null?'':(''+v);}
                function ob(v){if(v==null){return null;}if(typeof v.ToString==='function'){try{return JSON.parse(v.ToString());}catch(e){return null;}}return v;}
                function iso(v){var d=s(v).replace(' ','T');return d===''?new Date().toISOString():d;}
                var headers=ob(payload.webhookHeaders)||{};
                var receivedToken=s(headers['asaas-access-token']);
                if(!secretEquals('webhook_auth_token',receivedToken)){throw new Error('Token de webhook Asaas invalido ou ausente.');}
                var evt=s(payload.event);
                var pay=ob(payload.payment)||{};
                var status=s(pay.status);
                var eventType='';
                if(evt==='PAYMENT_RECEIVED'||evt==='PAYMENT_CONFIRMED'){
                  eventType=(status==='RECEIVED_IN_CASH')?'paid_in_cash':'paid';
                }
                else if(evt==='PAYMENT_RECEIVED_IN_CASH_UNDONE'){eventType='paid_in_cash_undone';}
                else if(evt==='PAYMENT_DELETED'){eventType='cancelled';}
                else {eventType='updated';}
                var financialEntryId=s(pay.externalReference);
                result.value={
                  acEventType:eventType,
                  chargeId:s(pay.id),
                  financialEntryId:financialEntryId,
                  callbackToken:s(variables.tenantId)+'~'+financialEntryId,
                  amountPaid:s(pay.value),
                  paidAt:iso(s(pay.clientPaymentDate)||s(pay.paymentDate)||s(payload.dateCreated))
                };$js$,
                    updatedat = now()
                WHERE name = 'asaas-normalizar-webhook-pagamento';
                """);
        }

        public override void Down()
        {
            // Catalogo de convergencia; Down nao reverte (conectores e execucoes referenciam estes registros).
        }
    }
}
