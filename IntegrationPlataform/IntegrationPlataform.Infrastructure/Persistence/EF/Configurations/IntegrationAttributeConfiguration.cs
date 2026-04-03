using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntegrationPlataform.Infrastructure.Persistence.EF.Configurations
{
    public sealed class IntegrationAttributeConfiguration : IEntityTypeConfiguration<IntegrationAttribute>
    {
        public void Configure(EntityTypeBuilder<IntegrationAttribute> builder)
        {
            builder.ToTable("integrationattribute");

            builder.Property(entity => entity.Field)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(entity => entity.Label)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(entity => entity.Description)
                .HasMaxLength(500);

            builder.Property(entity => entity.Placeholder)
                .HasMaxLength(500);

            builder.Property(entity => entity.DefaultValue)
                .HasMaxLength(500);

            builder.Property(entity => entity.Group)
                .HasMaxLength(100);

            builder.HasOne(entity => entity.Integration)
                .WithMany(entity => entity.Attributes)
                .HasForeignKey(entity => entity.IntegrationId);
        }
    }
}
