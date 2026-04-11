using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntegrationPlataform.Infrastructure.Persistence.EF.Configurations
{
    public sealed class PipelineExampleConfiguration : IEntityTypeConfiguration<PipelineExample>
    {
        public void Configure(EntityTypeBuilder<PipelineExample> builder)
        {
            builder.ToTable("pipelineexample");

            builder.Property(entity => entity.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(entity => entity.Description)
                .HasMaxLength(500);

            builder.HasOne(entity => entity.Pipeline)
                .WithMany(entity => entity.Examples)
                .HasForeignKey(entity => entity.PipelineId);

            builder.HasIndex(entity => new { entity.PipelineId, entity.Name })
                .IsUnique();
        }
    }
}
