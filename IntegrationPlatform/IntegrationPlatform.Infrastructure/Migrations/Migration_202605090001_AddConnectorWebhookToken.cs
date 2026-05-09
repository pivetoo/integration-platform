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

            Execute.Sql("UPDATE connector SET webhooktoken = REPLACE(gen_random_uuid()::text, '-', '') WHERE webhooktoken IS NULL");

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
