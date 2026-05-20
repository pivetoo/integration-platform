using IntegrationPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntegrationPlatform.Infrastructure.Persistence.EF.Configurations
{
    public sealed class ServiceContractConfiguration : IEntityTypeConfiguration<ServiceContract>
    {
        public void Configure(EntityTypeBuilder<ServiceContract> builder)
        {
            builder.ToTable("servicecontract");

            builder.Property(entity => entity.Identifier)
                .IsRequired()
                .HasMaxLength(120);

            builder.Property(entity => entity.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(entity => entity.Description)
                .HasMaxLength(500);

            builder.Property(entity => entity.InputSchema)
                .HasColumnType("text");

            builder.Property(entity => entity.OutputSchema)
                .HasColumnType("text");

            builder.Property(entity => entity.CallbackSchema)
                .HasColumnType("text");

            builder.Property(entity => entity.HasCallback)
                .IsRequired();

            builder.Property(entity => entity.IsActive)
                .IsRequired();

            builder.Property(entity => entity.IsSystem)
                .IsRequired()
                .HasDefaultValue(false);

            builder.HasOne(entity => entity.IntegrationCategory)
                .WithMany()
                .HasForeignKey(entity => entity.IntegrationCategoryId);

            builder.HasIndex(entity => entity.Identifier).IsUnique();
        }
    }
}
