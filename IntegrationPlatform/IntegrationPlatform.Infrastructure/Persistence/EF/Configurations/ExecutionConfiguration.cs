using IntegrationPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntegrationPlatform.Infrastructure.Persistence.EF.Configurations
{
    public sealed class ExecutionConfiguration : IEntityTypeConfiguration<Execution>
    {
        public void Configure(EntityTypeBuilder<Execution> builder)
        {
            builder.ToTable("execution");

            builder.HasOne(entity => entity.Connector)
                .WithMany(entity => entity.Executions)
                .HasForeignKey(entity => entity.ConnectorId);

            builder.HasOne(entity => entity.Pipeline)
                .WithMany(entity => entity.Executions)
                .HasForeignKey(entity => entity.PipelineId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(entity => entity.ProcessingQueue)
                .WithMany(entity => entity.Executions)
                .HasForeignKey(entity => entity.ProcessingQueueId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(entity => entity.StartedAt);
        }
    }
}
