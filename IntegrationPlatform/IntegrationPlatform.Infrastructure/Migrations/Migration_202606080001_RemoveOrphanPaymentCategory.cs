using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations
{
    // Remove a categoria 'payment' generica, que ficou orfã após a separação em
    // 'contas-a-receber' e 'contas-a-pagar' (202606070001).
    // Idempotente: so remove se nenhuma integration ou servicecontract a referenciar.
    [Migration(202606080001)]
    public sealed class Migration_202606080001_RemoveOrphanPaymentCategory : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                DELETE FROM integrationcategory
                WHERE identifier = 'payment'
                  AND NOT EXISTS (SELECT 1 FROM integration WHERE integrationcategoryid = integrationcategory.id)
                  AND NOT EXISTS (SELECT 1 FROM servicecontract WHERE integrationcategoryid = integrationcategory.id);
            ");
        }

        public override void Down()
        {
            Execute.Sql(@"
                INSERT INTO integrationcategory (identifier, name, description, isactive, issystem, createdat, updatedat)
                SELECT 'payment', 'Pagamento', 'Provedores de pagamento e cobrança.', true, true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc'
                WHERE NOT EXISTS (SELECT 1 FROM integrationcategory WHERE identifier = 'payment');
            ");
        }
    }
}
