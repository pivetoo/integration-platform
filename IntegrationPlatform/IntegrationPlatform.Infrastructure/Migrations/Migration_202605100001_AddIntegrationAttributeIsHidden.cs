using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations
{
    [Migration(202605100001)]
    public sealed class Migration_202605100001_AddIntegrationAttributeIsHidden : Migration
    {
        public override void Up()
        {
            Alter.Table("integrationattribute")
                .AddColumn("ishidden").AsBoolean().NotNullable().WithDefaultValue(false);
        }

        public override void Down()
        {
            Delete.Column("ishidden").FromTable("integrationattribute");
        }
    }
}
