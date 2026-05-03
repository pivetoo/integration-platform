using IntegrationPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntegrationPlatform.Infrastructure.Persistence.EF.Configurations
{
    public sealed class ExecutionLogConfiguration : IEntityTypeConfiguration<ExecutionLog>
    {
        public void Configure(EntityTypeBuilder<ExecutionLog> builder)
        {
            builder.ToTable("executionlog");

            builder.Property(entity => entity.Message)
                .IsRequired();

            builder.HasOne(entity => entity.Execution)
                .WithMany(entity => entity.Logs)
                .HasForeignKey(entity => entity.ExecutionId);

            builder.HasOne(entity => entity.PipelineStep)
                .WithMany(entity => entity.ExecutionLogs)
                .HasForeignKey(entity => entity.PipelineStepId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
