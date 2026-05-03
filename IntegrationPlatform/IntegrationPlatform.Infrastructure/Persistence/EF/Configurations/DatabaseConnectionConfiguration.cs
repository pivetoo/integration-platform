using IntegrationPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntegrationPlatform.Infrastructure.Persistence.EF.Configurations
{
    public sealed class DatabaseConnectionConfiguration : IEntityTypeConfiguration<DatabaseConnection>
    {
        public void Configure(EntityTypeBuilder<DatabaseConnection> builder)
        {
            builder.ToTable("databaseconnection");

            builder.Property(entity => entity.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(entity => entity.Host)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(entity => entity.Database)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(entity => entity.Username)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(entity => entity.Password)
                .IsRequired()
                .HasMaxLength(500);
        }
    }
}
