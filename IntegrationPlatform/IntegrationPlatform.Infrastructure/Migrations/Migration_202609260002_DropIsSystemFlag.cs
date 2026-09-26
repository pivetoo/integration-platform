using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations
{
    [Migration(202609260002)]
    public sealed class Migration_202609260002_DropIsSystemFlag : Migration
    {
        public override void Up()
        {
            if (Schema.Table("integrationcategory").Column("issystem").Exists())
            {
                Delete.Column("issystem").FromTable("integrationcategory");
            }

            if (Schema.Table("servicecontract").Column("issystem").Exists())
            {
                Delete.Column("issystem").FromTable("servicecontract");
            }
        }

        public override void Down()
        {
            if (!Schema.Table("integrationcategory").Column("issystem").Exists())
            {
                Alter.Table("integrationcategory")
                    .AddColumn("issystem").AsBoolean().NotNullable().WithDefaultValue(false);
            }

            if (!Schema.Table("servicecontract").Column("issystem").Exists())
            {
                Alter.Table("servicecontract")
                    .AddColumn("issystem").AsBoolean().NotNullable().WithDefaultValue(false);
            }
        }
    }
}
