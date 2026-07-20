using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations.AssinaturaDigital.ZapSign
{
    // A descricao da integracao aparece no catalogo de contas do Mainstay para usuario final:
    // linguagem de produto, sem jargao (API, eventos, URL de webhook). A instrucao tecnica do
    // webhook migra para a descricao do atributo api_token, visivel apenas no momento de conectar.
    [Migration(202607200002)]
    public sealed class Migration_202607200002_AjustaDescricaoZapSign : Migration
    {
        public override void Up()
        {
            Execute.Sql("""
                UPDATE integration SET
                    description = 'Assinatura digital de documentos e contratos. Envie o PDF, os signatários assinam pelo link recebido por e-mail e o resultado (assinado ou recusado) volta automaticamente para o Mainstay.',
                    updatedat = now()
                WHERE identifier = 'zapsign';
                """);

            Execute.Sql("""
                UPDATE integrationattribute SET
                    description = 'Token de API da ZapSign (Configurações > Integrações da conta ZapSign). Depois de conectar, cadastre na ZapSign o webhook dos eventos doc_signed e doc_refused apontando para https://integrations.mainstay.com.br/api/webhooks/{tenantId}/zapsign — é ele que traz o retorno automático das assinaturas.',
                    updatedat = now()
                WHERE field = 'api_token'
                  AND integrationid = (SELECT id FROM integration WHERE identifier = 'zapsign');
                """);
        }

        public override void Down()
        {
            // Ajuste de texto; nao ha estado anterior a restaurar.
        }
    }
}
