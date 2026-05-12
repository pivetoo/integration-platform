using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations
{
    [Migration(202605120002)]
    public sealed class Migration_202605120002_AddPipelineIsTestPipeline : Migration
    {
        public override void Up()
        {
            Alter.Table("pipeline")
                .AddColumn("istestpipeline").AsBoolean().NotNullable().WithDefaultValue(false);

            Execute.Sql("UPDATE pipeline SET istestpipeline = true WHERE identifier LIKE '%-test-connection'");
        }

        public override void Down()
        {
            Delete.Column("istestpipeline").FromTable("pipeline");
        }
    }
}
