using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations
{
    [Migration(202605200004)]
    public sealed class Migration_202605200004_AutoBindIntegrationsToServices : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                INSERT INTO integrationservicecontract (integrationid, servicecontractid, isactive, createdat, updatedat)
                SELECT i.id, sc.id, true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc'
                FROM integration i
                INNER JOIN servicecontract sc ON sc.integrationcategoryid = i.integrationcategoryid
                WHERE NOT EXISTS (
                    SELECT 1 FROM integrationservicecontract isc
                    WHERE isc.integrationid = i.id AND isc.servicecontractid = sc.id
                );
            ");
        }

        public override void Down()
        {
        }
    }
}
