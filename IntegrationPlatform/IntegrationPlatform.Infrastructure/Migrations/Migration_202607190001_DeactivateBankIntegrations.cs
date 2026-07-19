using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations
{
    // Inativa temporariamente as integracoes bancarias de Contas a Receber (sicredi, itau,
    // santander, bb, inter) e Contas a Pagar (santander-pagamento). Os seeds usam
    // WHERE NOT EXISTS por identifier, entao re-execucoes de seed nao as reativam.
    [Migration(202607190001)]
    public sealed class Migration_202607190001_DeactivateBankIntegrations : Migration
    {
        private static readonly string[] Identifiers =
        {
            "sicredi",
            "itau",
            "santander",
            "bb",
            "inter",
            "santander-pagamento"
        };

        public override void Up()
        {
            foreach (var identifier in Identifiers)
            {
                Update.Table("integration")
                    .Set(new { isactive = false })
                    .Where(new { identifier });
            }
        }

        public override void Down()
        {
            foreach (var identifier in Identifiers)
            {
                Update.Table("integration")
                    .Set(new { isactive = true })
                    .Where(new { identifier });
            }
        }
    }
}
