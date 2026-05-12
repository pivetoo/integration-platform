using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations
{
    [Migration(202605120001)]
    public sealed class Migration_202605120001_AddPipelineIsDefault : Migration
    {
        public override void Up()
        {
            Alter.Table("pipeline")
                .AddColumn("isdefault").AsBoolean().NotNullable().WithDefaultValue(false);
        }

        public override void Down()
        {
            Delete.Column("isdefault").FromTable("pipeline");
        }
    }
}
