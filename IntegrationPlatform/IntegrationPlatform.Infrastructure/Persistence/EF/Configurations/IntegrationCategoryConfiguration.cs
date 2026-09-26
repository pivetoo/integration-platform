using IntegrationPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntegrationPlatform.Infrastructure.Persistence.EF.Configurations
{
    public sealed class IntegrationCategoryConfiguration : IEntityTypeConfiguration<IntegrationCategory>
    {
        public void Configure(EntityTypeBuilder<IntegrationCategory> builder)
        {
            builder.ToTable("integrationcategory");

            builder.Property(entity => entity.Identifier)
                .IsRequired()
                .HasMaxLength(80);

            builder.Property(entity => entity.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(entity => entity.Description)
                .HasMaxLength(500);

            builder.HasIndex(entity => entity.Identifier).IsUnique();
        }
    }
}
