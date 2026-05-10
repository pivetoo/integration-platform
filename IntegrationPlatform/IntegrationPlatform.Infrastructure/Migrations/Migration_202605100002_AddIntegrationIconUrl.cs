using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations
{
    [Migration(202605100002)]
    public sealed class Migration_202605100002_AddIntegrationIconUrl : Migration
    {
        public override void Up()
        {
            Alter.Table("integration")
                .AddColumn("iconurl").AsString(500).Nullable();
        }

        public override void Down()
        {
            Delete.Column("iconurl").FromTable("integration");
        }
    }
}
