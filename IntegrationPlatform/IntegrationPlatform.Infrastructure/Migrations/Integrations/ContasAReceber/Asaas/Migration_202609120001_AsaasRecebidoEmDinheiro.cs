using FluentMigrator;
using IntegrationPlatform.Infrastructure.Migrations.Integrations;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations.ContasAReceber.Asaas
{
    // Recebido EM DINHEIRO no Asaas: o cliente pagou por fora (PIX direto, TED, especie) e alguem marcou a
    // cobranca como recebida la. Ate aqui o Mainstay ignorava: o webhook nao assinava o evento e, mesmo que
    // assinasse, o normalizador jogava tudo que nao fosse RECEIVED/CONFIRMED em "updated", que nao faz nada.
    //
    // Consequencia: a regua continuava cobrando quem ja tinha pagado e o aging contava um recebivel que nao
    // existia mais. Por isso passa a dar baixa — marcada como dinheiro que NAO passou pelo provedor, para a
    // conciliacao bancaria nao procurar um credito que nunca vem.
    //
    // O desfazimento (PAYMENT_RECEIVED_IN_CASH_UNDONE) vem junto de proposito: a marcacao e manual, logo
    // erravel, e aceitar a baixa sem aceitar a correcao transformaria um clique errado no provedor num
    // recebimento fantasma permanente.
    //
    // Estorno e chargeback seguem caindo em "updated" — sao outra decisao (o dinheiro de fato voltou) e
    // exigem contrapartida contabil, que e assunto separado.
    //
    // Atualiza os registros no lugar em vez de apagar e re-semear: pipelinestep referencia apicall e
    // javascriptfunction por id, e o delete quebraria a FK dos passos ja publicados.
    [Migration(202609120001)]
    public sealed class Migration_202609120001_AsaasRecebidoEmDinheiro : IntegrationSeedMigration
    {
        private const string EventosAssinados =
            "[\"PAYMENT_RECEIVED\",\"PAYMENT_CONFIRMED\",\"PAYMENT_DELETED\",\"PAYMENT_RECEIVED_IN_CASH\",\"PAYMENT_RECEIVED_IN_CASH_UNDONE\"]";

        public override void Up()
        {
            foreach (string nome in new[] { "Asaas - Criar webhook", "Asaas - Atualizar webhook" })
            {
                Execute.Sql($"""
                    UPDATE apicall
                    SET bodytemplate = replace(
                            bodytemplate,
                            '["PAYMENT_RECEIVED","PAYMENT_CONFIRMED","PAYMENT_DELETED"]',
                            '{EventosAssinados}'),
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
                var eventType='';
                if(evt==='PAYMENT_RECEIVED'||evt==='PAYMENT_CONFIRMED'){eventType='paid';}
                else if(evt==='PAYMENT_RECEIVED_IN_CASH'){eventType='paid_in_cash';}
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
