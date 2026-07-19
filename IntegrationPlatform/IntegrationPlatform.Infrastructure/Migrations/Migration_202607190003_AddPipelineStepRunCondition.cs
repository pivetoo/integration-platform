using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations
{
    // Adiciona pipelinestep.runcondition: expressao JavaScript booleana avaliada antes do step
    // (com variables/payload/attributes no escopo). Falsy = step pulado sem erro. NULL = sempre roda.
    [Migration(202607190003)]
    public sealed class Migration_202607190003_AddPipelineStepRunCondition : Migration
    {
        public override void Up()
        {
            if (!Schema.Table("pipelinestep").Column("runcondition").Exists())
            {
                Alter.Table("pipelinestep")
                    .AddColumn("runcondition").AsString(1000).Nullable();
            }
        }

        public override void Down()
        {
            if (Schema.Table("pipelinestep").Column("runcondition").Exists())
            {
                Delete.Column("runcondition").FromTable("pipelinestep");
            }
        }
    }
}
