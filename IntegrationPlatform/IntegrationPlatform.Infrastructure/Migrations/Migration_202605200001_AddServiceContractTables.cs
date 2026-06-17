using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations
{
    [Migration(202605200001)]
    public sealed class Migration_202605200001_AddServiceContractTables : Migration
    {
        public override void Up()
        {
            if (!Schema.Table("servicecontract").Exists())
            {
                Create.Table("servicecontract")
                    .WithColumn("id").AsInt64().PrimaryKey().Identity()
                    .WithColumn("identifier").AsString(120).NotNullable()
                    .WithColumn("name").AsString(200).NotNullable()
                    .WithColumn("description").AsString(500).Nullable()
                    .WithColumn("integrationcategoryid").AsInt64().NotNullable()
                    .WithColumn("inputschema").AsString(int.MaxValue).Nullable()
                    .WithColumn("outputschema").AsString(int.MaxValue).Nullable()
                    .WithColumn("hascallback").AsBoolean().NotNullable().WithDefaultValue(false)
                    .WithColumn("callbackschema").AsString(int.MaxValue).Nullable()
                    .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                    .WithColumn("issystem").AsBoolean().NotNullable().WithDefaultValue(false)
                    .WithColumn("createdat").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
                    .WithColumn("updatedat").AsDateTime().Nullable();

                Create.ForeignKey("fk_servicecontract_integrationcategory")
                    .FromTable("servicecontract").ForeignColumn("integrationcategoryid")
                    .ToTable("integrationcategory").PrimaryColumn("id");

                Create.Index("ux_servicecontract_identifier")
                    .OnTable("servicecontract")
                    .OnColumn("identifier").Ascending()
                    .WithOptions().Unique();

                Create.Index("ix_servicecontract_integrationcategoryid")
                    .OnTable("servicecontract")
                    .OnColumn("integrationcategoryid").Ascending();
            }

            if (!Schema.Table("integrationservicecontract").Exists())
            {
                Create.Table("integrationservicecontract")
                    .WithColumn("id").AsInt64().PrimaryKey().Identity()
                    .WithColumn("integrationid").AsInt64().NotNullable()
                    .WithColumn("servicecontractid").AsInt64().NotNullable()
                    .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                    .WithColumn("createdat").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
                    .WithColumn("updatedat").AsDateTime().Nullable();

                Create.ForeignKey("fk_integrationservicecontract_integration")
                    .FromTable("integrationservicecontract").ForeignColumn("integrationid")
                    .ToTable("integration").PrimaryColumn("id");

                Create.ForeignKey("fk_integrationservicecontract_servicecontract")
                    .FromTable("integrationservicecontract").ForeignColumn("servicecontractid")
                    .ToTable("servicecontract").PrimaryColumn("id");

                Create.Index("ux_integrationservicecontract_pair")
                    .OnTable("integrationservicecontract")
                    .OnColumn("integrationid").Ascending()
                    .OnColumn("servicecontractid").Ascending()
                    .WithOptions().Unique();

                Create.Index("ix_integrationservicecontract_integrationid")
                    .OnTable("integrationservicecontract")
                    .OnColumn("integrationid").Ascending();

                Create.Index("ix_integrationservicecontract_servicecontractid")
                    .OnTable("integrationservicecontract")
                    .OnColumn("servicecontractid").Ascending();
            }

            if (!Schema.Table("pipeline").Column("servicecontractid").Exists())
            {
                Alter.Table("pipeline")
                    .AddColumn("servicecontractid").AsInt64().Nullable()
                        .ForeignKey("fk_pipeline_servicecontract", "servicecontract", "id");

                Create.Index("ix_pipeline_servicecontractid")
                    .OnTable("pipeline")
                    .OnColumn("servicecontractid").Ascending();
            }

            // Categoria 'messaging' (legado): seed condicional mantido em SQL.
            Execute.Sql(@"
                INSERT INTO integrationcategory (identifier, name, description, isactive, issystem, createdat, updatedat)
                SELECT 'messaging', 'Mensageria', 'Provedores de envio de mensagens (WhatsApp, SMS).', true, true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc'
                WHERE NOT EXISTS (SELECT 1 FROM integrationcategory WHERE identifier = 'messaging');
            ");
        }

        public override void Down()
        {
            Execute.Sql("DELETE FROM integrationcategory WHERE identifier = 'messaging' AND issystem = true;");

            if (Schema.Table("pipeline").Column("servicecontractid").Exists())
            {
                Delete.Index("ix_pipeline_servicecontractid").OnTable("pipeline");
                Delete.Column("servicecontractid").FromTable("pipeline");
            }

            if (Schema.Table("integrationservicecontract").Exists())
            {
                Delete.Table("integrationservicecontract");
            }

            if (Schema.Table("servicecontract").Exists())
            {
                Delete.Table("servicecontract");
            }
        }
    }
}
