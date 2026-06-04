using IntegrationPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntegrationPlatform.Infrastructure.Persistence.EF.Configurations
{
    public sealed class CallbackDeliveryConfiguration : IEntityTypeConfiguration<CallbackDelivery>
    {
        public void Configure(EntityTypeBuilder<CallbackDelivery> builder)
        {
            builder.ToTable("callbackdelivery");

            builder.HasIndex(entity => new { entity.Status, entity.NextAttemptAt });
        }
    }
}
