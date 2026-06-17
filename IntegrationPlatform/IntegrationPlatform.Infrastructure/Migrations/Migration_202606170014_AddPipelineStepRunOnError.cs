using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations
{
    // Adiciona pipelinestep.runonerror: marca steps que rodam SOMENTE quando o pipeline parou por erro
    // (ex.: callback de falha). O motor pula esses no fluxo normal e os executa num passo de tratamento
    // de erro, expondo {{errorMessage}} no contexto.
    [Migration(202606170014)]
    public sealed class Migration_202606170014_AddPipelineStepRunOnError : Migration
    {
        public override void Up()
        {
            if (!Schema.Table("pipelinestep").Column("runonerror").Exists())
            {
                Alter.Table("pipelinestep")
                    .AddColumn("runonerror").AsBoolean().NotNullable().WithDefaultValue(false);
            }
        }

        public override void Down()
        {
            if (Schema.Table("pipelinestep").Column("runonerror").Exists())
            {
                Delete.Column("runonerror").FromTable("pipelinestep");
            }
        }
    }
}
