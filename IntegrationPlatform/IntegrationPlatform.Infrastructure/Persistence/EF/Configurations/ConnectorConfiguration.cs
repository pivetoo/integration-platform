using IntegrationPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntegrationPlatform.Infrastructure.Persistence.EF.Configurations
{
    public sealed class ConnectorConfiguration : IEntityTypeConfiguration<Connector>
    {
        public void Configure(EntityTypeBuilder<Connector> builder)
        {
            builder.ToTable("connector");

            builder.Property(entity => entity.SystemApplicationId)
                .HasMaxLength(500);

            builder.Property(entity => entity.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(entity => entity.WebhookToken)
                .HasMaxLength(64);

            builder.HasIndex(entity => entity.WebhookToken)
                .IsUnique()
                .HasDatabaseName("ix_connector_webhooktoken");

            builder.Property(entity => entity.CallbackUrl)
                .HasMaxLength(500);

            builder.Property(entity => entity.CallbackToken)
                .HasMaxLength(120);

            builder.HasOne(entity => entity.Integration)
                .WithMany(entity => entity.Connectors)
                .HasForeignKey(entity => entity.IntegrationId);
        }
    }
}
