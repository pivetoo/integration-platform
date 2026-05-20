using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations
{
    [Migration(202605200002)]
    public sealed class Migration_202605200002_AddConnectorCallback : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                ALTER TABLE connector
                    ADD COLUMN IF NOT EXISTS callbackurl VARCHAR(500) NULL,
                    ADD COLUMN IF NOT EXISTS callbacktoken VARCHAR(120) NULL;
            ");
        }

        public override void Down()
        {
            Execute.Sql(@"
                ALTER TABLE connector
                    DROP COLUMN IF EXISTS callbacktoken,
                    DROP COLUMN IF EXISTS callbackurl;
            ");
        }
    }
}
