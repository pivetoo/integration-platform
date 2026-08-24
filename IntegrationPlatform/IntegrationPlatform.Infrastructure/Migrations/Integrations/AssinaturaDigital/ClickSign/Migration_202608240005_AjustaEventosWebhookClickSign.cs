using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations.AssinaturaDigital.ClickSign
{
    // A Migration_202608240004 listava so "sign e refusal" na instrucao do webhook, mas o pipeline
    // clicksign-webhook (funcao clicksign-normalizar-webhook) trata 7 eventos: sign, refusal, close,
    // auto_close, document_closed, cancel e upload - os tres de fechamento (close/auto_close/
    // document_closed) sao redundantes de proposito, o ClickSign dispara um ou outro dependendo de
    // como o documento e concluido, e os tres levam a "assinado" no Mainstay.
    [Migration(202608240005)]
    public sealed class Migration_202608240005_AjustaEventosWebhookClickSign : Migration
    {
        public override void Up()
        {
            Execute.Sql("""
                UPDATE integrationattribute SET
                    description = 'Access Token gerado em Configurações > API da conta ClickSign. Depois de conectar, cadastre na ClickSign o webhook dos eventos sign, refusal, close, auto_close, document_closed, cancel e upload (Configurações > API > Adicionar Webhook) apontando para https://integrations.mainstay.com.br/api/webhooks/{tenantId}/clicksign — é ele que traz o retorno automático das assinaturas.',
                    updatedat = now()
                WHERE field = 'api_token'
                  AND integrationid = (SELECT id FROM integration WHERE identifier = 'clicksign');
                """);
        }

        public override void Down()
        {
            // Ajuste de texto; nao ha estado anterior a restaurar.
        }
    }
}
