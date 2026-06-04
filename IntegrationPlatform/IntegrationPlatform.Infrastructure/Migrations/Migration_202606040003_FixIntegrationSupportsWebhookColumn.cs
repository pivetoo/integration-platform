using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations
{
    // A migration 202605110003 adicionou a coluna supportswebhook na tabela ERRADA ("integrations",
    // plural) - a tabela real e "integration" (singular), que e a que o EF mapeia (IntegrationConfiguration
    // .ToTable("integration")). Em bancos novos a coluna nunca foi criada na tabela certa, e qualquer
    // INSERT/UPDATE de Integration falha (42703: column "supportswebhook" does not exist). Corrige
    // adicionando na tabela correta de forma idempotente (no-op se ja existir, ex.: em producao).
    [Migration(202606040003)]
    public sealed class Migration_202606040003_FixIntegrationSupportsWebhookColumn : Migration
    {
        public override void Up()
        {
            Execute.Sql("ALTER TABLE integration ADD COLUMN IF NOT EXISTS supportswebhook BOOLEAN NOT NULL DEFAULT false;");
        }

        public override void Down()
        {
            Execute.Sql("ALTER TABLE integration DROP COLUMN IF EXISTS supportswebhook;");
        }
    }
}
