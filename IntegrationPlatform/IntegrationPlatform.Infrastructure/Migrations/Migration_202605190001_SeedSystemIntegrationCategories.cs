using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations
{
    // Garante que as categorias de sistema (email, digital-signature) existam num banco LIMPO.
    // Sem isso, a migration 202605200001 (seed de service contracts) falha ao referenciar a categoria
    // 'email'/'digital-signature' inexistente (integrationcategoryid NULL viola NOT NULL), abortando toda
    // a cadeia de migrations. A 202605180001 so faz UPDATE em categorias ja existentes (por nome), o que
    // num banco novo nao atinge nenhuma linha. Producao ja tinha as categorias (seed fora da migration),
    // entao aqui e idempotente (no-op onde ja existem). 'banking' ja e semeado pela 202605180001 e
    // 'messaging' pela 202605200001.
    [Migration(202605190001)]
    public sealed class Migration_202605190001_SeedSystemIntegrationCategories : Migration
    {
        public override void Up()
        {
            SeedCategory("email", "E-mail", "Provedores de envio de email transacional.");
            SeedCategory("digital-signature", "Assinatura digital", "Provedores de coleta de assinaturas digitais.");
        }

        public override void Down()
        {
        }

        private void SeedCategory(string identifier, string name, string description)
        {
            Execute.Sql($@"
                INSERT INTO integrationcategory (identifier, name, description, isactive, issystem, createdat, updatedat)
                SELECT '{identifier}', '{name}', '{description}', true, true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc'
                WHERE NOT EXISTS (SELECT 1 FROM integrationcategory WHERE identifier = '{identifier}');
            ");
        }
    }
}
