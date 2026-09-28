using FluentMigrator;

namespace IntegrationPlatform.Infrastructure.Migrations
{
    [Migration(202609280001)]
    public sealed class Migration_202609280001_AddQueueRetryAndIdempotency : Migration
    {
        private const string IdempotencyIndex = "ux_processingqueue_idempotency";

        public override void Up()
        {
            if (!Schema.Table("processingqueue").Column("attempts").Exists())
            {
                Alter.Table("processingqueue")
                    .AddColumn("attempts").AsInt32().NotNullable().WithDefaultValue(0);
            }

            if (!Schema.Table("processingqueue").Column("idempotencykey").Exists())
            {
                Alter.Table("processingqueue")
                    .AddColumn("idempotencykey").AsString(200).Nullable();
            }

            if (!Schema.Table("pipeline").Column("maxattempts").Exists())
            {
                Alter.Table("pipeline")
                    .AddColumn("maxattempts").AsInt32().NotNullable().WithDefaultValue(1);
            }

            if (!Schema.Table("processingqueue").Index(IdempotencyIndex).Exists())
            {
                Execute.Sql($"CREATE UNIQUE INDEX {IdempotencyIndex} ON processingqueue (connectorid, pipelineid, idempotencykey) WHERE idempotencykey IS NOT NULL");
            }
        }

        public override void Down()
        {
            if (Schema.Table("processingqueue").Index(IdempotencyIndex).Exists())
            {
                Delete.Index(IdempotencyIndex).OnTable("processingqueue");
            }

            if (Schema.Table("processingqueue").Column("idempotencykey").Exists())
            {
                Delete.Column("idempotencykey").FromTable("processingqueue");
            }

            if (Schema.Table("processingqueue").Column("attempts").Exists())
            {
                Delete.Column("attempts").FromTable("processingqueue");
            }

            if (Schema.Table("pipeline").Column("maxattempts").Exists())
            {
                Delete.Column("maxattempts").FromTable("pipeline");
            }
        }
    }
}
