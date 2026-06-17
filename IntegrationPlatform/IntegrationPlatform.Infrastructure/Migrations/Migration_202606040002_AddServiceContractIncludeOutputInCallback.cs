using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations
{
    [Migration(202606040002)]
    public sealed class Migration_202606040002_AddServiceContractIncludeOutputInCallback : Migration
    {
        public override void Up()
        {
            if (!Schema.Table("servicecontract").Column("includeoutputincallback").Exists())
            {
                Alter.Table("servicecontract")
                    .AddColumn("includeoutputincallback").AsBoolean().NotNullable().WithDefaultValue(false);
            }
        }

        public override void Down()
        {
            if (Schema.Table("servicecontract").Column("includeoutputincallback").Exists())
            {
                Delete.Column("includeoutputincallback").FromTable("servicecontract");
            }
        }
    }
}
