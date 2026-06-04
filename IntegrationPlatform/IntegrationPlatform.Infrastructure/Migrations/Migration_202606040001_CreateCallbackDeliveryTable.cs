using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations
{
    [Migration(202606040001)]
    public sealed class Migration_202606040001_CreateCallbackDeliveryTable : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                CREATE TABLE IF NOT EXISTS callbackdelivery (
                    id BIGSERIAL PRIMARY KEY,
                    executionid BIGINT NOT NULL,
                    connectorid BIGINT NOT NULL,
                    serviceidentifier VARCHAR(120) NOT NULL,
                    callbackurl VARCHAR(2000) NOT NULL,
                    callbacktoken VARCHAR(255) NULL,
                    payload TEXT NOT NULL,
                    status INT NOT NULL DEFAULT 1,
                    attempts INT NOT NULL DEFAULT 0,
                    nextattemptat TIMESTAMPTZ NULL,
                    deliveredat TIMESTAMPTZ NULL,
                    lasterror TEXT NULL,
                    createdat TIMESTAMPTZ NOT NULL DEFAULT now(),
                    updatedat TIMESTAMPTZ NULL
                );
            ");

            Execute.Sql("CREATE INDEX IF NOT EXISTS ix_callbackdelivery_status_nextattemptat ON callbackdelivery(status, nextattemptat);");
        }

        public override void Down()
        {
            Execute.Sql("DROP INDEX IF EXISTS ix_callbackdelivery_status_nextattemptat;");
            Execute.Sql("DROP TABLE IF EXISTS callbackdelivery;");
        }
    }
}
