using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntegrationPlataform.Infrastructure.Persistence.EF.Configurations
{
    public sealed class PipelineStepValueMappingConfiguration : IEntityTypeConfiguration<PipelineStepValueMapping>
    {
        public void Configure(EntityTypeBuilder<PipelineStepValueMapping> builder)
        {
            builder.ToTable("pipelinestepvaluemapping");

            builder.Property(entity => entity.TargetField)
                .IsRequired()
                .HasMaxLength(300);

            builder.Property(entity => entity.SourceType)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(entity => entity.SourcePath)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(entity => entity.ValueType)
                .IsRequired()
                .HasMaxLength(50);

            builder.HasOne(entity => entity.PipelineStep)
                .WithMany(entity => entity.ValueMappings)
                .HasForeignKey(entity => entity.PipelineStepId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(entity => entity.SourcePipelineStep)
                .WithMany(entity => entity.SourceMappings)
                .HasForeignKey(entity => entity.SourcePipelineStepId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(entity => entity.PipelineExample)
                .WithMany()
                .HasForeignKey(entity => entity.PipelineExampleId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(entity => new { entity.PipelineStepId, entity.Order });
        }
    }
}
