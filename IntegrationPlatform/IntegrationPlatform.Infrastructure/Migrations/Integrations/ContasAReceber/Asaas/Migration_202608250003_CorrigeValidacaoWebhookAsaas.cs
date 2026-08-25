using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations.ContasAReceber.Asaas
{
    // Correcao: a validacao original de asaas-normalizar-webhook-pagamento lia variables.webhook_auth_token,
    // mas atributos SENSIVEIS do conector nunca sao expostos ao escopo do JS (StepExecutorService filtra
    // SensitiveAttributeFields de proposito, para nao vazar segredo no step do usuario) - a comparacao
    // sempre falhava (expectedToken vazio), rejeitando todo webhook real da Asaas mesmo com o token certo.
    // Confirmado ao vivo: execution 48 (tenant Agencia de Testes) recebeu PAYMENT_RECEIVED real da Asaas
    // com o asaas-access-token correto e foi rejeitada com "Token de webhook Asaas invalido ou ausente.".
    // Fix: engine.secretEquals(field, candidate) (novo em StepExecutorService), que compara em tempo
    // constante SEM nunca expor o valor sensivel ao JS.
    [Migration(202608250003)]
    public sealed class Migration_202608250003_CorrigeValidacaoWebhookAsaas : Migration
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
                result.value={
                  acEventType:eventType,
                  chargeId:s(pay.id),
                  financialEntryId:s(pay.externalReference),
                  amountPaid:Number(pay.value)||0,
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
