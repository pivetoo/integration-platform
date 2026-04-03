using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntegrationPlataform.Infrastructure.Persistence.EF.Configurations
{
    public sealed class PipelineStepConfiguration : IEntityTypeConfiguration<PipelineStep>
    {
        public void Configure(EntityTypeBuilder<PipelineStep> builder)
        {
            builder.ToTable("pipelinestep");

            builder.Property(entity => entity.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.HasOne(entity => entity.Pipeline)
                .WithMany(entity => entity.Steps)
                .HasForeignKey(entity => entity.PipelineId);

            builder.HasOne(entity => entity.ApiCall)
                .WithMany()
                .HasForeignKey(entity => entity.ApiCallId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(entity => entity.JavaScriptFunction)
                .WithMany()
                .HasForeignKey(entity => entity.JavaScriptFunctionId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(entity => entity.DatabaseScript)
                .WithMany()
                .HasForeignKey(entity => entity.DatabaseScriptId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(entity => new { entity.PipelineId, entity.Order })
                .IsUnique();
        }
    }
}
