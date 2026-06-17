using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations
{
    [Migration(202605180001)]
    public sealed class Migration_202605180001_AddIntegrationCategoryIdentifierAndIsSystem : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                ALTER TABLE integrationcategory
                    ADD COLUMN IF NOT EXISTS identifier VARCHAR(80) NULL,
                    ADD COLUMN IF NOT EXISTS issystem BOOLEAN NOT NULL DEFAULT false;
            ");

            Execute.Sql(@"
                ALTER TABLE integrationcategory
                    ALTER COLUMN identifier SET NOT NULL;
            ");

            Execute.Sql("CREATE UNIQUE INDEX IF NOT EXISTS ux_integrationcategory_identifier ON integrationcategory(identifier);");
        }

        public override void Down()
        {
            Execute.Sql("DROP INDEX IF EXISTS ux_integrationcategory_identifier;");
            Execute.Sql(@"
                ALTER TABLE integrationcategory
                    DROP COLUMN IF EXISTS issystem,
                    DROP COLUMN IF EXISTS identifier;
            ");
        }
    }
}
