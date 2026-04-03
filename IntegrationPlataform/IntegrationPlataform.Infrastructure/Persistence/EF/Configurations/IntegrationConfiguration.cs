using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntegrationPlataform.Infrastructure.Persistence.EF.Configurations
{
    public sealed class IntegrationConfiguration : IEntityTypeConfiguration<Integration>
    {
        public void Configure(EntityTypeBuilder<Integration> builder)
        {
            builder.ToTable("integration");

            builder.Property(entity => entity.Identifier)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(entity => entity.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(entity => entity.Description)
                .HasMaxLength(500);

            builder.HasOne(entity => entity.IntegrationCategory)
                .WithMany(entity => entity.Integrations)
                .HasForeignKey(entity => entity.IntegrationCategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(entity => entity.Identifier)
                .IsUnique();
        }
    }
}
