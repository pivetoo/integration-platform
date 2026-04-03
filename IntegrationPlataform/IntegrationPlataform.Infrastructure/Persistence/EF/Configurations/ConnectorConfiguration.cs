using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntegrationPlataform.Infrastructure.Persistence.EF.Configurations
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

            builder.HasOne(entity => entity.Integration)
                .WithMany(entity => entity.Connectors)
                .HasForeignKey(entity => entity.IntegrationId);
        }
    }
}
