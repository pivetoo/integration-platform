using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntegrationPlataform.Infrastructure.Persistence.EF.Configurations
{
    public sealed class ReferenceConfiguration : IEntityTypeConfiguration<Reference>
    {
        public void Configure(EntityTypeBuilder<Reference> builder)
        {
            builder.ToTable("reference");

            builder.Property(entity => entity.EntityName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(entity => entity.InternalId)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(entity => entity.ExternalId)
                .IsRequired()
                .HasMaxLength(500);

            builder.HasOne(entity => entity.Connector)
                .WithMany(entity => entity.References)
                .HasForeignKey(entity => entity.ConnectorId);

            builder.HasIndex(entity => new { entity.ConnectorId, entity.EntityName, entity.InternalId })
                .HasDatabaseName("ix_reference_lookup");
        }
    }
}
