using IntegrationPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntegrationPlatform.Infrastructure.Persistence.EF.Configurations
{
    public sealed class DatabaseScriptConfiguration : IEntityTypeConfiguration<DatabaseScript>
    {
        public void Configure(EntityTypeBuilder<DatabaseScript> builder)
        {
            builder.ToTable("databasescript");

            builder.Property(entity => entity.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(entity => entity.Description)
                .HasMaxLength(500);

            builder.Property(entity => entity.Script)
                .IsRequired();

            builder.HasOne(entity => entity.DatabaseConnection)
                .WithMany(entity => entity.Scripts)
                .HasForeignKey(entity => entity.DatabaseConnectionId);
        }
    }
}
