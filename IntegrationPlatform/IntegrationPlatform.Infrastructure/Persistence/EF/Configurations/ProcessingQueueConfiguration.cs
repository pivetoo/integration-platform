using IntegrationPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntegrationPlatform.Infrastructure.Persistence.EF.Configurations
{
    public sealed class ProcessingQueueConfiguration : IEntityTypeConfiguration<ProcessingQueue>
    {
        public void Configure(EntityTypeBuilder<ProcessingQueue> builder)
        {
            builder.ToTable("processingqueue");

            builder.HasOne(entity => entity.Connector)
                .WithMany(entity => entity.ProcessingQueues)
                .HasForeignKey(entity => entity.ConnectorId);

            builder.HasOne(entity => entity.Pipeline)
                .WithMany(entity => entity.ProcessingQueues)
                .HasForeignKey(entity => entity.PipelineId);

            builder.HasIndex(entity => new { entity.Status, entity.ScheduledAt });
        }
    }
}
