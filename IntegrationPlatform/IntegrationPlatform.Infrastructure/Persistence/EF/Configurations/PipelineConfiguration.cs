using IntegrationPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntegrationPlatform.Infrastructure.Persistence.EF.Configurations
{
    public sealed class PipelineConfiguration : IEntityTypeConfiguration<Pipeline>
    {
        public void Configure(EntityTypeBuilder<Pipeline> builder)
        {
            builder.ToTable("pipeline");

            builder.Property(entity => entity.Identifier)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(entity => entity.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(entity => entity.Description)
                .HasMaxLength(500);

            builder.HasOne(entity => entity.Integration)
                .WithMany(entity => entity.Pipelines)
                .HasForeignKey(entity => entity.IntegrationId);

            builder.Property(entity => entity.IsDefault).IsRequired();

            builder.HasIndex(entity => new { entity.IntegrationId, entity.Identifier })
                .IsUnique();
        }
    }
}
