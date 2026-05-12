using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations
{
    [Migration(202605110003)]
    public sealed class Migration_202605110003_AddIntegrationSupportsWebhook : Migration
    {
        public override void Up()
        {
            if (!Schema.Table("integrations").Column("supportswebhook").Exists())
            {
                Alter.Table("integrations")
                    .AddColumn("supportswebhook").AsBoolean().NotNullable().WithDefaultValue(false);
            }
        }

        public override void Down()
        {
            if (Schema.Table("integrations").Column("supportswebhook").Exists())
            {
                Delete.Column("supportswebhook").FromTable("integrations");
            }
        }
    }
}
