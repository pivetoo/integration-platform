using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations
{
    [Migration(202605180001)]
    public sealed class Migration_202605180001_AddIntegrationCategoryIdentifierAndIsSystem : Migration
    {
        public override void Up()
        {
            if (!Schema.Table("integrationcategory").Column("identifier").Exists())
            {
                Alter.Table("integrationcategory")
                    .AddColumn("identifier").AsString(80).Nullable();
            }

            if (!Schema.Table("integrationcategory").Column("issystem").Exists())
            {
                Alter.Table("integrationcategory")
                    .AddColumn("issystem").AsBoolean().NotNullable().WithDefaultValue(false);
            }

            Alter.Table("integrationcategory")
                .AlterColumn("identifier").AsString(80).NotNullable();

            if (!Schema.Table("integrationcategory").Index("ux_integrationcategory_identifier").Exists())
            {
                Create.Index("ux_integrationcategory_identifier")
                    .OnTable("integrationcategory")
                    .OnColumn("identifier").Unique();
            }
        }

        public override void Down()
        {
            if (Schema.Table("integrationcategory").Index("ux_integrationcategory_identifier").Exists())
            {
                Delete.Index("ux_integrationcategory_identifier").OnTable("integrationcategory");
            }

            if (Schema.Table("integrationcategory").Column("issystem").Exists())
            {
                Delete.Column("issystem").FromTable("integrationcategory");
            }

            if (Schema.Table("integrationcategory").Column("identifier").Exists())
            {
                Delete.Column("identifier").FromTable("integrationcategory");
            }
        }
    }
}
