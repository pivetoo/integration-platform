using FluentMigrator;

namespace IntegrationPlataform.Infrastructure.Migrations
{
    [Migration(202604060001)]
    public sealed class Migration_202604060001_AddPipelineExampleAndValueMappingTables : Migration
    {
        public override void Up()
        {
            Create.Table("pipelineexample")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("pipelineid").AsInt64().NotNullable()
                .WithColumn("name").AsString(200).NotNullable()
                .WithColumn("description").AsString(500).Nullable()
                .WithColumn("inputpayloadexample").AsString(int.MaxValue).Nullable()
                .WithColumn("expectedoutputexample").AsString(int.MaxValue).Nullable()
                .WithColumn("isdefault").AsBoolean().NotNullable().WithDefaultValue(false)
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fk_pipelineexample_pipeline_pipelineid")
                .FromTable("pipelineexample").ForeignColumn("pipelineid")
                .ToTable("pipeline").PrimaryColumn("id");

            Create.Index("ix_pipelineexample_pipelineid_name")
                .OnTable("pipelineexample")
                .OnColumn("pipelineid").Ascending()
                .OnColumn("name").Ascending()
                .WithOptions().Unique();

            Create.Table("pipelinestepexample")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("pipelinestepid").AsInt64().NotNullable()
                .WithColumn("pipelineexampleid").AsInt64().Nullable()
                .WithColumn("name").AsString(200).NotNullable()
                .WithColumn("requestexample").AsString(int.MaxValue).Nullable()
                .WithColumn("responseexample").AsString(int.MaxValue).Nullable()
                .WithColumn("isdefault").AsBoolean().NotNullable().WithDefaultValue(false)
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fk_pipelinestepexample_pipelinestep_pipelinestepid")
                .FromTable("pipelinestepexample").ForeignColumn("pipelinestepid")
                .ToTable("pipelinestep").PrimaryColumn("id");

            Create.ForeignKey("fk_pipelinestepexample_pipelineexample_pipelineexampleid")
                .FromTable("pipelinestepexample").ForeignColumn("pipelineexampleid")
                .ToTable("pipelineexample").PrimaryColumn("id");

            Create.Table("pipelinestepvaluemapping")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("pipelinestepid").AsInt64().NotNullable()
                .WithColumn("sourcepipelinestepid").AsInt64().Nullable()
                .WithColumn("pipelineexampleid").AsInt64().Nullable()
                .WithColumn("targetfield").AsString(300).NotNullable()
                .WithColumn("sourcetype").AsString(50).NotNullable()
                .WithColumn("sourcepath").AsString(500).NotNullable()
                .WithColumn("valuetype").AsString(50).NotNullable()
                .WithColumn("fixedvalue").AsString(int.MaxValue).Nullable()
                .WithColumn("order").AsInt32().NotNullable()
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fk_pipelinestepvaluemapping_pipelinestep_pipelinestepid")
                .FromTable("pipelinestepvaluemapping").ForeignColumn("pipelinestepid")
                .ToTable("pipelinestep").PrimaryColumn("id");

            Create.ForeignKey("fk_pipelinestepvaluemapping_pipelinestep_sourcepipelinestepid")
                .FromTable("pipelinestepvaluemapping").ForeignColumn("sourcepipelinestepid")
                .ToTable("pipelinestep").PrimaryColumn("id");

            Create.ForeignKey("fk_pipelinestepvaluemapping_pipelineexample_pipelineexampleid")
                .FromTable("pipelinestepvaluemapping").ForeignColumn("pipelineexampleid")
                .ToTable("pipelineexample").PrimaryColumn("id");

            Create.Index("ix_pipelinestepvaluemapping_pipelinestepid_order")
                .OnTable("pipelinestepvaluemapping")
                .OnColumn("pipelinestepid").Ascending()
                .OnColumn("order").Ascending();
        }

        public override void Down()
        {
            Delete.Table("pipelinestepvaluemapping");
            Delete.Table("pipelinestepexample");
            Delete.Table("pipelineexample");
        }
    }
}
