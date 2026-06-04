using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations
{
    [Migration(202606040002)]
    public sealed class Migration_202606040002_AddServiceContractIncludeOutputInCallback : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                ALTER TABLE servicecontract
                    ADD COLUMN IF NOT EXISTS includeoutputincallback BOOLEAN NOT NULL DEFAULT false;
            ");
        }

        public override void Down()
        {
            Execute.Sql("ALTER TABLE servicecontract DROP COLUMN IF EXISTS includeoutputincallback;");
        }
    }
}
