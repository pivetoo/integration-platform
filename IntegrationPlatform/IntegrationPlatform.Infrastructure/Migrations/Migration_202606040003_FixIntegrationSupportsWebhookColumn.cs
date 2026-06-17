using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations
{
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
