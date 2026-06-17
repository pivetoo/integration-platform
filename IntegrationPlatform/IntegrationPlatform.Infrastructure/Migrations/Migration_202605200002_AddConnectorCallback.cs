using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations
{
    [Migration(202605200002)]
    public sealed class Migration_202605200002_AddConnectorCallback : Migration
    {
        public override void Up()
        {
            if (!Schema.Table("connector").Column("callbackurl").Exists())
            {
                Alter.Table("connector")
                    .AddColumn("callbackurl").AsString(500).Nullable();
            }

            if (!Schema.Table("connector").Column("callbacktoken").Exists())
            {
                Alter.Table("connector")
                    .AddColumn("callbacktoken").AsString(120).Nullable();
            }
        }

        public override void Down()
        {
            if (Schema.Table("connector").Column("callbacktoken").Exists())
            {
                Delete.Column("callbacktoken").FromTable("connector");
            }

            if (Schema.Table("connector").Column("callbackurl").Exists())
            {
                Delete.Column("callbackurl").FromTable("connector");
            }
        }
    }
}
