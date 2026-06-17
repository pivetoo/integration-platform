using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations
{
    [Migration(202606040001)]
    public sealed class Migration_202606040001_CreateCallbackDeliveryTable : Migration
    {
        public override void Up()
        {
            if (!Schema.Table("callbackdelivery").Exists())
            {
                Create.Table("callbackdelivery")
                    .WithColumn("id").AsInt64().PrimaryKey().Identity()
                    .WithColumn("executionid").AsInt64().NotNullable()
                    .WithColumn("connectorid").AsInt64().NotNullable()
                    .WithColumn("serviceidentifier").AsString(120).NotNullable()
                    .WithColumn("callbackurl").AsString(2000).NotNullable()
                    .WithColumn("callbacktoken").AsString(255).Nullable()
                    .WithColumn("payload").AsString(int.MaxValue).NotNullable()
                    .WithColumn("status").AsInt32().NotNullable().WithDefaultValue(1)
                    .WithColumn("attempts").AsInt32().NotNullable().WithDefaultValue(0)
                    .WithColumn("nextattemptat").AsDateTimeOffset().Nullable()
                    .WithColumn("deliveredat").AsDateTimeOffset().Nullable()
                    .WithColumn("lasterror").AsString(int.MaxValue).Nullable()
                    .WithColumn("createdat").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
                    .WithColumn("updatedat").AsDateTimeOffset().Nullable();

                Create.Index("ix_callbackdelivery_status_nextattemptat")
                    .OnTable("callbackdelivery")
                    .OnColumn("status").Ascending()
                    .OnColumn("nextattemptat").Ascending();
            }
        }

        public override void Down()
        {
            if (Schema.Table("callbackdelivery").Exists())
            {
                Delete.Table("callbackdelivery");
            }
        }
    }
}
