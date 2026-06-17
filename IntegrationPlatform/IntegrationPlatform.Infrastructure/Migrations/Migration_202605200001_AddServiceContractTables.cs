using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations
{
    [Migration(202605200001)]
    public sealed class Migration_202605200001_AddServiceContractTables : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                CREATE TABLE IF NOT EXISTS servicecontract (
                    id BIGSERIAL PRIMARY KEY,
                    identifier VARCHAR(120) NOT NULL,
                    name VARCHAR(200) NOT NULL,
                    description VARCHAR(500) NULL,
                    integrationcategoryid BIGINT NOT NULL REFERENCES integrationcategory(id),
                    inputschema TEXT NULL,
                    outputschema TEXT NULL,
                    hascallback BOOLEAN NOT NULL DEFAULT false,
                    callbackschema TEXT NULL,
                    isactive BOOLEAN NOT NULL DEFAULT true,
                    issystem BOOLEAN NOT NULL DEFAULT false,
                    createdat TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
                    updatedat TIMESTAMP NULL
                );
            ");

            Execute.Sql("CREATE UNIQUE INDEX IF NOT EXISTS ux_servicecontract_identifier ON servicecontract(identifier);");
            Execute.Sql("CREATE INDEX IF NOT EXISTS ix_servicecontract_integrationcategoryid ON servicecontract(integrationcategoryid);");

            Execute.Sql(@"
                CREATE TABLE IF NOT EXISTS integrationservicecontract (
                    id BIGSERIAL PRIMARY KEY,
                    integrationid BIGINT NOT NULL REFERENCES integration(id),
                    servicecontractid BIGINT NOT NULL REFERENCES servicecontract(id),
                    isactive BOOLEAN NOT NULL DEFAULT true,
                    createdat TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
                    updatedat TIMESTAMP NULL
                );
            ");

            Execute.Sql("CREATE UNIQUE INDEX IF NOT EXISTS ux_integrationservicecontract_pair ON integrationservicecontract(integrationid, servicecontractid);");
            Execute.Sql("CREATE INDEX IF NOT EXISTS ix_integrationservicecontract_integrationid ON integrationservicecontract(integrationid);");
            Execute.Sql("CREATE INDEX IF NOT EXISTS ix_integrationservicecontract_servicecontractid ON integrationservicecontract(servicecontractid);");

            Execute.Sql(@"
                ALTER TABLE pipeline
                    ADD COLUMN IF NOT EXISTS servicecontractid BIGINT NULL REFERENCES servicecontract(id);
            ");

            Execute.Sql("CREATE INDEX IF NOT EXISTS ix_pipeline_servicecontractid ON pipeline(servicecontractid);");

            Execute.Sql(@"
                INSERT INTO integrationcategory (identifier, name, description, isactive, issystem, createdat, updatedat)
                SELECT 'messaging', 'Mensageria', 'Provedores de envio de mensagens (WhatsApp, SMS).', true, true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc'
                WHERE NOT EXISTS (SELECT 1 FROM integrationcategory WHERE identifier = 'messaging');
            ");
        }

        public override void Down()
        {
            Execute.Sql("DROP INDEX IF EXISTS ix_pipeline_servicecontractid;");
            Execute.Sql("ALTER TABLE pipeline DROP COLUMN IF EXISTS servicecontractid;");

            Execute.Sql("DROP INDEX IF EXISTS ix_integrationservicecontract_servicecontractid;");
            Execute.Sql("DROP INDEX IF EXISTS ix_integrationservicecontract_integrationid;");
            Execute.Sql("DROP INDEX IF EXISTS ux_integrationservicecontract_pair;");
            Execute.Sql("DROP TABLE IF EXISTS integrationservicecontract;");

            Execute.Sql("DROP INDEX IF EXISTS ix_servicecontract_integrationcategoryid;");
            Execute.Sql("DROP INDEX IF EXISTS ux_servicecontract_identifier;");
            Execute.Sql("DROP TABLE IF EXISTS servicecontract;");

            Execute.Sql("DELETE FROM integrationcategory WHERE identifier = 'messaging' AND issystem = true;");
        }
    }
}
