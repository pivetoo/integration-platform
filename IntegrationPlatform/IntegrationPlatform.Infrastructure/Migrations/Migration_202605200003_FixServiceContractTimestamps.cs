using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations
{
    [Migration(202605200003)]
    public sealed class Migration_202605200003_FixServiceContractTimestamps : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                ALTER TABLE servicecontract
                    ALTER COLUMN createdat TYPE TIMESTAMPTZ USING createdat AT TIME ZONE 'UTC',
                    ALTER COLUMN updatedat TYPE TIMESTAMPTZ USING updatedat AT TIME ZONE 'UTC';
            ");

            Execute.Sql(@"
                ALTER TABLE integrationservicecontract
                    ALTER COLUMN createdat TYPE TIMESTAMPTZ USING createdat AT TIME ZONE 'UTC',
                    ALTER COLUMN updatedat TYPE TIMESTAMPTZ USING updatedat AT TIME ZONE 'UTC';
            ");
        }

        public override void Down()
        {
            Execute.Sql(@"
                ALTER TABLE servicecontract
                    ALTER COLUMN createdat TYPE TIMESTAMP USING createdat AT TIME ZONE 'UTC',
                    ALTER COLUMN updatedat TYPE TIMESTAMP USING updatedat AT TIME ZONE 'UTC';
            ");

            Execute.Sql(@"
                ALTER TABLE integrationservicecontract
                    ALTER COLUMN createdat TYPE TIMESTAMP USING createdat AT TIME ZONE 'UTC',
                    ALTER COLUMN updatedat TYPE TIMESTAMP USING updatedat AT TIME ZONE 'UTC';
            ");
        }
    }
}
