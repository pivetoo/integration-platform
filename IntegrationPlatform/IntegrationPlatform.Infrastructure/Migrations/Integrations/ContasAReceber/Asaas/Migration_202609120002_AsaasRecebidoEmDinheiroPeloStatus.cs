using FluentMigrator;
using IntegrationPlatform.Infrastructure.Migrations.Integrations;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations.ContasAReceber.Asaas
{
    // Corrige a 202609120001, que assinava PAYMENT_RECEIVED_IN_CASH: esse evento NAO existe no Asaas, e a API
    // recusa o registro inteiro com "O evento [PAYMENT_RECEIVED_IN_CASH] e invalido" — ou seja, a versao
    // anterior nao so deixava de cobrir o caso como derrubava o cadastro do webhook.
    //
    // Ao marcar uma cobranca como recebida em dinheiro, o Asaas dispara PAYMENT_RECEIVED com
    // payment.status = RECEIVED_IN_CASH. So o desfazimento tem evento proprio
    // (PAYMENT_RECEIVED_IN_CASH_UNDONE), que e valido. Entao a origem do recebimento passa a ser lida do
    // STATUS do payload, que e o campo autoritativo, em vez do nome do evento.
    [Migration(202609120002)]
    public sealed class Migration_202609120002_AsaasRecebidoEmDinheiroPeloStatus : IntegrationSeedMigration
    {
        public override void Up()
        {
            foreach (string nome in new[] { "Asaas - Criar webhook", "Asaas - Atualizar webhook" })
            {
                Execute.Sql($"""
                    UPDATE apicall
                    SET bodytemplate = replace(
                            bodytemplate,
                            '["PAYMENT_RECEIVED","PAYMENT_CONFIRMED","PAYMENT_DELETED","PAYMENT_RECEIVED_IN_CASH","PAYMENT_RECEIVED_IN_CASH_UNDONE"]',
                            '["PAYMENT_RECEIVED","PAYMENT_CONFIRMED","PAYMENT_DELETED","PAYMENT_RECEIVED_IN_CASH_UNDONE"]'),
                        updatedat = now()
                    WHERE name = '{nome}';
                    """);
            }

            Execute.Sql("""
                UPDATE javascriptfunction
                SET code = $js$function s(v){return v==null?'':(''+v);}
                function ob(v){if(v==null){return null;}if(typeof v.ToString==='function'){try{return JSON.parse(v.ToString());}catch(e){return null;}}return v;}
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
                  paidAt:(pay.clientPaymentDate||pay.paymentDate||null)
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
