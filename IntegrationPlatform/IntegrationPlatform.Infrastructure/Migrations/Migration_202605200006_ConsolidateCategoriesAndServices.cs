using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations
{
    [Migration(202605200006)]
    public sealed class Migration_202605200006_ConsolidateCategoriesAndServices : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                INSERT INTO integrationservicecontract (integrationid, servicecontractid, isactive, createdat, updatedat)
                SELECT i.id, sc.id, true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc'
                FROM integration i
                INNER JOIN servicecontract sc ON sc.integrationcategoryid = i.integrationcategoryid
                WHERE NOT EXISTS (SELECT 1 FROM integrationservicecontract isc WHERE isc.integrationid = i.id AND isc.servicecontractid = sc.id);
            ");

            Execute.Sql(@"
                UPDATE pipeline SET servicecontractid = (SELECT id FROM servicecontract WHERE identifier = 'whatsapp.send'), isdefault = true
                WHERE servicecontractid IS NULL AND identifier LIKE '%-send-message';
            ");
            Execute.Sql(@"
                UPDATE pipeline SET servicecontractid = (SELECT id FROM servicecontract WHERE identifier = 'payment.transfer.create'), isdefault = true
                WHERE servicecontractid IS NULL AND identifier LIKE '%-send-pix';
            ");
            Execute.Sql(@"
                UPDATE pipeline SET servicecontractid = (SELECT id FROM servicecontract WHERE identifier = 'payment.charge.create'), isdefault = true
                WHERE servicecontractid IS NULL AND (identifier = 'gerarCobranca' OR identifier LIKE '%-create-charge' OR identifier LIKE '%-create-cobranca');
            ");

            // So remove a categoria se NENHUMA integration E NENHUM servicecontract a referenciam.
            // Num banco limpo, 'messaging.send' segue apontando para 'messaging' (a consolidacao para
            // 'whatsapp' nao roda porque a categoria 'whatsapp' nao existe), entao sem o guard de
            // servicecontract o DELETE violaria a FK servicecontract_integrationcategoryid_fkey.
            Execute.Sql(@"
                DELETE FROM integrationcategory
                WHERE identifier = 'messaging'
                  AND NOT EXISTS (SELECT 1 FROM integration WHERE integrationcategoryid = integrationcategory.id)
                  AND NOT EXISTS (SELECT 1 FROM servicecontract WHERE integrationcategoryid = integrationcategory.id);
            ");
        }

        public override void Down()
        {
        }
    }
}
