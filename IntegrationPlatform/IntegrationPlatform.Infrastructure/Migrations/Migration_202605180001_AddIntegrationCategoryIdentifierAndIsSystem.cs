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
                UPDATE integrationcategory
                SET identifier = 'payment', issystem = true
                WHERE identifier IS NULL AND LOWER(name) IN ('pagamento', 'pagamentos', 'payment', 'payments');
            ");

            Execute.Sql(@"
                UPDATE integrationcategory
                SET identifier = 'digital-signature', issystem = true
                WHERE identifier IS NULL AND LOWER(name) IN ('assinatura digital', 'assinatura', 'digital signature', 'signature', 'firma digital');
            ");

            Execute.Sql(@"
                UPDATE integrationcategory
                SET identifier = 'email', issystem = true
                WHERE identifier IS NULL AND LOWER(name) IN ('e-mail', 'email', 'correo electronico', 'correo electrónico');
            ");

            Execute.Sql(@"
                UPDATE integrationcategory
                SET identifier = TRIM(BOTH '-' FROM REGEXP_REPLACE(LOWER(name), '[^a-z0-9]+', '-', 'g'))
                WHERE identifier IS NULL;
            ");

            Execute.Sql(@"
                UPDATE integrationcategory
                SET identifier = identifier || '-' || id
                WHERE identifier IN (
                    SELECT identifier FROM integrationcategory GROUP BY identifier HAVING COUNT(*) > 1
                );
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
