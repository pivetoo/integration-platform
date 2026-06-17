using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations
{
    [Migration(202605090001)]
    public sealed class Migration_202605090001_AddConnectorWebhookToken : Migration
    {
        public override void Up()
        {
            Alter.Table("connector")
                .AddColumn("webhooktoken").AsString(64).Nullable();

            Create.Index("ix_connector_webhooktoken")
                .OnTable("connector")
                .OnColumn("webhooktoken").Unique();
        }

        public override void Down()
        {
            Delete.Index("ix_connector_webhooktoken").OnTable("connector");
            Delete.Column("webhooktoken").FromTable("connector");
        }
    }
}
