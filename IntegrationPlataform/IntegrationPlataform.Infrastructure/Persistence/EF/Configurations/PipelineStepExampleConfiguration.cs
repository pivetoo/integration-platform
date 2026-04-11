using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntegrationPlataform.Infrastructure.Persistence.EF.Configurations
{
    public sealed class PipelineStepExampleConfiguration : IEntityTypeConfiguration<PipelineStepExample>
    {
        public void Configure(EntityTypeBuilder<PipelineStepExample> builder)
        {
            builder.ToTable("pipelinestepexample");

            builder.Property(entity => entity.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.HasOne(entity => entity.PipelineStep)
                .WithMany(entity => entity.Examples)
                .HasForeignKey(entity => entity.PipelineStepId);

            builder.HasOne(entity => entity.PipelineExample)
                .WithMany(entity => entity.StepExamples)
                .HasForeignKey(entity => entity.PipelineExampleId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
