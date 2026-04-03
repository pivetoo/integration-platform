using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntegrationPlataform.Infrastructure.Persistence.EF.Configurations
{
    public sealed class PipelineRoutineConfiguration : IEntityTypeConfiguration<PipelineRoutine>
    {
        public void Configure(EntityTypeBuilder<PipelineRoutine> builder)
        {
            builder.ToTable("pipelineroutine");

            builder.HasOne(entity => entity.Connector)
                .WithMany(entity => entity.Routines)
                .HasForeignKey(entity => entity.ConnectorId);

            builder.HasOne(entity => entity.Pipeline)
                .WithMany(entity => entity.Routines)
                .HasForeignKey(entity => entity.PipelineId);

            builder.HasIndex(entity => new { entity.ConnectorId, entity.PipelineId })
                .IsUnique();
        }
    }
}
