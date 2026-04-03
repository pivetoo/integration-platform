using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntegrationPlataform.Infrastructure.Persistence.EF.Configurations
{
    public sealed class ConnectorAttributeValueConfiguration : IEntityTypeConfiguration<ConnectorAttributeValue>
    {
        public void Configure(EntityTypeBuilder<ConnectorAttributeValue> builder)
        {
            builder.ToTable("connectorattributevalue");

            builder.Property(entity => entity.Value)
                .IsRequired();

            builder.HasOne(entity => entity.Connector)
                .WithMany(entity => entity.AttributeValues)
                .HasForeignKey(entity => entity.ConnectorId);

            builder.HasOne(entity => entity.IntegrationAttribute)
                .WithMany(entity => entity.ConnectorAttributeValues)
                .HasForeignKey(entity => entity.IntegrationAttributeId);

            builder.HasIndex(entity => new { entity.ConnectorId, entity.IntegrationAttributeId })
                .IsUnique();
        }
    }
}
