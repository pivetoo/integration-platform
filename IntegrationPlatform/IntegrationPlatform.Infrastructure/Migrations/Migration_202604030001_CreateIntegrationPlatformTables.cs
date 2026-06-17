using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations
{
    [Migration(202604030001)]
    public sealed class Migration_202604030001_CreateIntegrationPlatformTables : Migration
    {
        public override void Up()
        {
            if (Schema.Table("integrationcategory").Exists())
            {
                return;
            }

            Create.Table("integrationcategory")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("name").AsString(100).NotNullable()
                .WithColumn("description").AsString(500).Nullable()
                .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.Table("integration")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("identifier").AsString(50).NotNullable()
                .WithColumn("name").AsString(100).NotNullable()
                .WithColumn("description").AsString(500).Nullable()
                .WithColumn("integrationcategoryid").AsInt64().Nullable()
                .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fk_integration_integrationcategory_integrationcategoryid")
                .FromTable("integration").ForeignColumn("integrationcategoryid")
                .ToTable("integrationcategory").PrimaryColumn("id");

            Create.Index("ix_integration_identifier")
                .OnTable("integration")
                .OnColumn("identifier").Ascending()
                .WithOptions().Unique();

            Create.Table("integrationattribute")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("integrationid").AsInt64().NotNullable()
                .WithColumn("field").AsString(100).NotNullable()
                .WithColumn("label").AsString(100).NotNullable()
                .WithColumn("description").AsString(500).Nullable()
                .WithColumn("placeholder").AsString(500).Nullable()
                .WithColumn("type").AsInt32().NotNullable()
                .WithColumn("defaultvalue").AsString(500).Nullable()
                .WithColumn("isrequired").AsBoolean().NotNullable().WithDefaultValue(false)
                .WithColumn("order").AsInt32().NotNullable()
                .WithColumn("group").AsString(100).Nullable()
                .WithColumn("issensitive").AsBoolean().NotNullable().WithDefaultValue(false)
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fk_integrationattribute_integration_integrationid")
                .FromTable("integrationattribute").ForeignColumn("integrationid")
                .ToTable("integration").PrimaryColumn("id");

            Create.Table("connector")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("systemapplicationid").AsString(500).Nullable()
                .WithColumn("integrationid").AsInt64().NotNullable()
                .WithColumn("name").AsString(200).NotNullable()
                .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fk_connector_integration_integrationid")
                .FromTable("connector").ForeignColumn("integrationid")
                .ToTable("integration").PrimaryColumn("id");

            Create.Table("connectorattributevalue")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("connectorid").AsInt64().NotNullable()
                .WithColumn("integrationattributeid").AsInt64().NotNullable()
                .WithColumn("value").AsString(int.MaxValue).NotNullable()
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fk_connectorattributevalue_connector_connectorid")
                .FromTable("connectorattributevalue").ForeignColumn("connectorid")
                .ToTable("connector").PrimaryColumn("id");

            Create.ForeignKey("fk_connectorattributevalue_integrationattribute_integrationattributeid")
                .FromTable("connectorattributevalue").ForeignColumn("integrationattributeid")
                .ToTable("integrationattribute").PrimaryColumn("id");

            Create.Index("ix_connectorattributevalue_connectorid_integrationattributeid")
                .OnTable("connectorattributevalue")
                .OnColumn("connectorid").Ascending()
                .OnColumn("integrationattributeid").Ascending()
                .WithOptions().Unique();

            Create.Table("apicall")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("name").AsString(200).NotNullable()
                .WithColumn("description").AsString(500).Nullable()
                .WithColumn("method").AsInt32().NotNullable()
                .WithColumn("url").AsString(2000).NotNullable()
                .WithColumn("headerstemplate").AsString(int.MaxValue).Nullable()
                .WithColumn("bodytemplate").AsString(int.MaxValue).Nullable()
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.Table("javascriptfunction")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("name").AsString(200).NotNullable()
                .WithColumn("description").AsString(500).Nullable()
                .WithColumn("code").AsString(int.MaxValue).NotNullable()
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.Table("databaseconnection")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("name").AsString(100).NotNullable()
                .WithColumn("type").AsInt32().NotNullable()
                .WithColumn("host").AsString(200).NotNullable()
                .WithColumn("port").AsInt32().NotNullable()
                .WithColumn("database").AsString(100).NotNullable()
                .WithColumn("username").AsString(100).NotNullable()
                .WithColumn("password").AsString(500).NotNullable()
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.Table("databasescript")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("databaseconnectionid").AsInt64().NotNullable()
                .WithColumn("name").AsString(200).NotNullable()
                .WithColumn("description").AsString(500).Nullable()
                .WithColumn("script").AsString(int.MaxValue).NotNullable()
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fk_databasescript_databaseconnection_databaseconnectionid")
                .FromTable("databasescript").ForeignColumn("databaseconnectionid")
                .ToTable("databaseconnection").PrimaryColumn("id");

            Create.Table("pipeline")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("integrationid").AsInt64().NotNullable()
                .WithColumn("identifier").AsString(100).NotNullable()
                .WithColumn("name").AsString(200).NotNullable()
                .WithColumn("description").AsString(500).Nullable()
                .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fk_pipeline_integration_integrationid")
                .FromTable("pipeline").ForeignColumn("integrationid")
                .ToTable("integration").PrimaryColumn("id");

            Create.Index("ix_pipeline_integrationid_identifier")
                .OnTable("pipeline")
                .OnColumn("integrationid").Ascending()
                .OnColumn("identifier").Ascending()
                .WithOptions().Unique();

            Create.Table("pipelinestep")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("pipelineid").AsInt64().NotNullable()
                .WithColumn("order").AsInt32().NotNullable()
                .WithColumn("name").AsString(200).NotNullable()
                .WithColumn("type").AsInt32().NotNullable()
                .WithColumn("apicallid").AsInt64().Nullable()
                .WithColumn("javascriptfunctionid").AsInt64().Nullable()
                .WithColumn("databasescriptid").AsInt64().Nullable()
                .WithColumn("erroraction").AsInt32().NotNullable()
                .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("ignoreonresponse").AsBoolean().NotNullable().WithDefaultValue(false)
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fk_pipelinestep_pipeline_pipelineid")
                .FromTable("pipelinestep").ForeignColumn("pipelineid")
                .ToTable("pipeline").PrimaryColumn("id");

            Create.ForeignKey("fk_pipelinestep_apicall_apicallid")
                .FromTable("pipelinestep").ForeignColumn("apicallid")
                .ToTable("apicall").PrimaryColumn("id");

            Create.ForeignKey("fk_pipelinestep_javascriptfunction_javascriptfunctionid")
                .FromTable("pipelinestep").ForeignColumn("javascriptfunctionid")
                .ToTable("javascriptfunction").PrimaryColumn("id");

            Create.ForeignKey("fk_pipelinestep_databasescript_databasescriptid")
                .FromTable("pipelinestep").ForeignColumn("databasescriptid")
                .ToTable("databasescript").PrimaryColumn("id");

            Create.Index("ix_pipelinestep_pipelineid_order")
                .OnTable("pipelinestep")
                .OnColumn("pipelineid").Ascending()
                .OnColumn("order").Ascending()
                .WithOptions().Unique();

            Create.Table("processingqueue")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("connectorid").AsInt64().NotNullable()
                .WithColumn("pipelineid").AsInt64().NotNullable()
                .WithColumn("priority").AsInt32().NotNullable()
                .WithColumn("status").AsInt32().NotNullable()
                .WithColumn("payload").AsString(int.MaxValue).Nullable()
                .WithColumn("lasterror").AsString(int.MaxValue).Nullable()
                .WithColumn("scheduledat").AsDateTimeOffset().Nullable()
                .WithColumn("startedat").AsDateTimeOffset().Nullable()
                .WithColumn("finishedat").AsDateTimeOffset().Nullable()
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fk_processingqueue_connector_connectorid")
                .FromTable("processingqueue").ForeignColumn("connectorid")
                .ToTable("connector").PrimaryColumn("id");

            Create.ForeignKey("fk_processingqueue_pipeline_pipelineid")
                .FromTable("processingqueue").ForeignColumn("pipelineid")
                .ToTable("pipeline").PrimaryColumn("id");

            Create.Index("ix_processingqueue_status_scheduledat")
                .OnTable("processingqueue")
                .OnColumn("status").Ascending()
                .OnColumn("scheduledat").Ascending();

            Create.Table("execution")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("type").AsInt32().NotNullable()
                .WithColumn("connectorid").AsInt64().NotNullable()
                .WithColumn("pipelineid").AsInt64().Nullable()
                .WithColumn("processingqueueid").AsInt64().Nullable()
                .WithColumn("status").AsInt32().NotNullable()
                .WithColumn("inputdata").AsString(int.MaxValue).Nullable()
                .WithColumn("outputdata").AsString(int.MaxValue).Nullable()
                .WithColumn("errors").AsString(int.MaxValue).Nullable()
                .WithColumn("startedat").AsDateTimeOffset().NotNullable()
                .WithColumn("finishedat").AsDateTimeOffset().Nullable()
                .WithColumn("duration").AsInt64().Nullable()
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fk_execution_connector_connectorid")
                .FromTable("execution").ForeignColumn("connectorid")
                .ToTable("connector").PrimaryColumn("id");

            Create.ForeignKey("fk_execution_pipeline_pipelineid")
                .FromTable("execution").ForeignColumn("pipelineid")
                .ToTable("pipeline").PrimaryColumn("id");

            Create.ForeignKey("fk_execution_processingqueue_processingqueueid")
                .FromTable("execution").ForeignColumn("processingqueueid")
                .ToTable("processingqueue").PrimaryColumn("id");

            Create.Index("ix_execution_startedat")
                .OnTable("execution")
                .OnColumn("startedat").Ascending();

            Create.Table("executionlog")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("executionid").AsInt64().NotNullable()
                .WithColumn("pipelinestepid").AsInt64().Nullable()
                .WithColumn("level").AsInt32().NotNullable()
                .WithColumn("message").AsString(int.MaxValue).NotNullable()
                .WithColumn("context").AsString(int.MaxValue).Nullable()
                .WithColumn("request").AsString(int.MaxValue).Nullable()
                .WithColumn("response").AsString(int.MaxValue).Nullable()
                .WithColumn("httpstatuscode").AsInt32().Nullable()
                .WithColumn("duration").AsInt64().Nullable()
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fk_executionlog_execution_executionid")
                .FromTable("executionlog").ForeignColumn("executionid")
                .ToTable("execution").PrimaryColumn("id");

            Create.ForeignKey("fk_executionlog_pipelinestep_pipelinestepid")
                .FromTable("executionlog").ForeignColumn("pipelinestepid")
                .ToTable("pipelinestep").PrimaryColumn("id");

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

            Create.Table("pipelineroutine")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("connectorid").AsInt64().NotNullable()
                .WithColumn("pipelineid").AsInt64().NotNullable()
                .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("intervalinminutes").AsInt32().NotNullable()
                .WithColumn("defaultpayload").AsString(int.MaxValue).Nullable()
                .WithColumn("lastexecutionat").AsDateTimeOffset().Nullable()
                .WithColumn("nextexecutionat").AsDateTimeOffset().Nullable()
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fk_pipelineroutine_connector_connectorid")
                .FromTable("pipelineroutine").ForeignColumn("connectorid")
                .ToTable("connector").PrimaryColumn("id");

            Create.ForeignKey("fk_pipelineroutine_pipeline_pipelineid")
                .FromTable("pipelineroutine").ForeignColumn("pipelineid")
                .ToTable("pipeline").PrimaryColumn("id");

            Create.Index("ix_pipelineroutine_connectorid_pipelineid")
                .OnTable("pipelineroutine")
                .OnColumn("connectorid").Ascending()
                .OnColumn("pipelineid").Ascending()
                .WithOptions().Unique();
        }

        public override void Down()
        {
            if (!Schema.Table("integrationcategory").Exists())
            {
                return;
            }

            Delete.Table("pipelineroutine");
            Delete.Table("reference");
            Delete.Table("executionlog");
            Delete.Table("execution");
            Delete.Table("processingqueue");
            Delete.Table("pipelinestep");
            Delete.Table("pipeline");
            Delete.Table("databasescript");
            Delete.Table("databaseconnection");
            Delete.Table("javascriptfunction");
            Delete.Table("apicall");
            Delete.Table("connectorattributevalue");
            Delete.Table("connector");
            Delete.Table("integrationattribute");
            Delete.Table("integration");
            Delete.Table("integrationcategory");
        }
    }
}
