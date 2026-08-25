using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations.Integrations.AssinaturaDigital.D4Sign
{
    // D4Sign nunca foi validada ponta a ponta (falta liberacao de credencial pelo suporte deles) -
    // inativa por enquanto pra nao aparecer como opcao usavel no catalogo de Assinatura Digital
    // enquanto ZapSign e ClickSign, ja validadas ao vivo, seguem ativas.
    [Migration(202608250001)]
    public sealed class Migration_202608250001_InativaD4Sign : Migration
    {
        public override void Up()
        {
            Execute.Sql("""
                UPDATE integration SET
                    isactive = false,
                    updatedat = now()
                WHERE identifier = 'd4sign';
                """);
        }

        public override void Down()
        {
            Execute.Sql("""
                UPDATE integration SET
                    isactive = true,
                    updatedat = now()
                WHERE identifier = 'd4sign';
                """);
        }
    }
}
