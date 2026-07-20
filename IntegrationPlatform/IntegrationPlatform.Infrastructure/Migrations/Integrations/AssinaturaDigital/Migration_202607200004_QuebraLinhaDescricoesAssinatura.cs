using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations.AssinaturaDigital
{
    // Reformata as descricoes longas dos atributos de assinatura digital em paragrafos com quebra
    // de linha (o modal de conexao do Mainstay preserva \n via whitespace-pre-line).
    [Migration(202607200004)]
    public sealed class Migration_202607200004_QuebraLinhaDescricoesAssinatura : Migration
    {
        public override void Up()
        {
            Execute.Sql("""
                UPDATE integrationattribute SET
                    description = E'Token de API da ZapSign (Configurações > Integrações da conta ZapSign).\n\nDepois de conectar, cadastre na ZapSign o webhook dos eventos doc_signed e doc_refused apontando para:\nhttps://integrations.mainstay.com.br/api/webhooks/{tenantId}/zapsign\n\nÉ ele que traz o retorno automático das assinaturas.',
                    updatedat = now()
                WHERE field = 'api_token'
                  AND integrationid = (SELECT id FROM integration WHERE identifier = 'zapsign');
                """);

            Execute.Sql("""
                UPDATE integrationattribute SET
                    description = E'Token de API da conta D4Sign (Configurações > API).\n\nAtenção: o limite padrão da D4Sign é de 10 requisições por hora e cada envio consome 4. Peça aumento de limite ao suporte da D4Sign antes de usar em produção.',
                    updatedat = now()
                WHERE field = 'tokenAPI'
                  AND integrationid = (SELECT id FROM integration WHERE identifier = 'd4sign');
                """);
        }

        public override void Down()
        {
            // Ajuste de texto; nao ha estado anterior a restaurar.
        }
    }
}
