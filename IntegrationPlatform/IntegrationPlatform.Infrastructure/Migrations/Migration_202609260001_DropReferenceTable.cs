using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations
{
    [Migration(202609260001)]
    public sealed class Migration_202609260001_DropReferenceTable : Migration
    {
        public override void Up()
        {
            if (Schema.Table("reference").Exists())
            {
                Delete.Table("reference");
            }
        }

        public override void Down()
        {
            if (Schema.Table("reference").Exists())
            {
                return;
            }

            Create.Table("reference")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("connectorid").AsInt64().NotNullable()
                .WithColumn("entityname").AsString(100).NotNullable()
                .WithColumn("internalid").AsString(500).NotNullable()
                .WithColumn("externalid").AsString(500).NotNullable()
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fk_reference_connector_connectorid")
                .FromTable("reference").ForeignColumn("connectorid")
                .ToTable("connector").PrimaryColumn("id");

            Create.Index("ix_reference_lookup")
                .OnTable("reference")
                .OnColumn("connectorid").Ascending()
                .OnColumn("entityname").Ascending()
                .OnColumn("internalid").Ascending();
        }
    }
}
