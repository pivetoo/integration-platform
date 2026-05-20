using IntegrationPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntegrationPlatform.Infrastructure.Persistence.EF.Configurations
{
    public sealed class IntegrationServiceContractConfiguration : IEntityTypeConfiguration<IntegrationServiceContract>
    {
        public void Configure(EntityTypeBuilder<IntegrationServiceContract> builder)
        {
            builder.ToTable("integrationservicecontract");

            builder.Property(entity => entity.IsActive)
                .IsRequired();

            builder.HasOne(entity => entity.Integration)
                .WithMany(entity => entity.ServiceContracts)
                .HasForeignKey(entity => entity.IntegrationId);

            builder.HasOne(entity => entity.ServiceContract)
                .WithMany(entity => entity.IntegrationServiceContracts)
                .HasForeignKey(entity => entity.ServiceContractId);

            builder.HasIndex(entity => new { entity.IntegrationId, entity.ServiceContractId })
                .IsUnique()
                .HasDatabaseName("ux_integrationservicecontract_pair");
        }
    }
}
