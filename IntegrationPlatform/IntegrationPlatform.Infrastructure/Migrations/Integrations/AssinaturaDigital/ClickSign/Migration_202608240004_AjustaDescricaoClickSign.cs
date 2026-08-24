using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations.AssinaturaDigital.ClickSign
{
    // Mesma correcao aplicada a ZapSign (Migration_202607200002): a descricao da integracao aparece
    // no catalogo de contas do Mainstay para o usuario final - linguagem de produto, sem jargao (API,
    // eventos, URL de webhook). A instrucao tecnica do webhook migra para a descricao do atributo
    // api_token, visivel apenas no momento de conectar.
    [Migration(202608240004)]
    public sealed class Migration_202608240004_AjustaDescricaoClickSign : Migration
    {
        public override void Up()
        {
            Execute.Sql("""
                UPDATE integration SET
                    description = 'Assinatura digital de documentos e contratos. Envie o PDF, os signatários assinam pelo link recebido por e-mail e o resultado (assinado ou recusado) volta automaticamente para o Mainstay.',
                    updatedat = now()
                WHERE identifier = 'clicksign';
                """);

            Execute.Sql("""
                UPDATE integrationattribute SET
                    description = 'Access Token gerado em Configurações > API da conta ClickSign. Depois de conectar, cadastre na ClickSign o webhook dos eventos sign e refusal (Configurações > API > Adicionar Webhook) apontando para https://integrations.mainstay.com.br/api/webhooks/{tenantId}/clicksign — é ele que traz o retorno automático das assinaturas.',
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
