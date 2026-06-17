using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations
{
    // Adiciona a coluna supportswebhook na tabela correta "integration" (singular), que e a que o EF
    // mapeia (IntegrationConfiguration.ToTable("integration")). A migration 202605110003 original criava
    // a coluna na tabela ERRADA ("integrations", plural, inexistente) e foi removida; este e o unico
    // ponto que cria a coluna. Idempotente (no-op se ja existir, ex.: em producao).
    [Migration(202606040003)]
    public sealed class Migration_202606040003_FixIntegrationSupportsWebhookColumn : Migration
    {
        public override void Up()
        {
            if (!Schema.Table("integration").Column("supportswebhook").Exists())
            {
                Alter.Table("integration")
                    .AddColumn("supportswebhook").AsBoolean().NotNullable().WithDefaultValue(false);
            }
        }

        public override void Down()
        {
            if (Schema.Table("integration").Column("supportswebhook").Exists())
            {
                Delete.Column("supportswebhook").FromTable("integration");
            }
        }
    }
}
