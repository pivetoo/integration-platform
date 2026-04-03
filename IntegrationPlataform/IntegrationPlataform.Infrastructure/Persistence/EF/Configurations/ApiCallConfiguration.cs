using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntegrationPlataform.Infrastructure.Persistence.EF.Configurations
{
    public sealed class ApiCallConfiguration : IEntityTypeConfiguration<ApiCall>
    {
        public void Configure(EntityTypeBuilder<ApiCall> builder)
        {
            builder.ToTable("apicall");

            builder.Property(entity => entity.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(entity => entity.Description)
                .HasMaxLength(500);

            builder.Property(entity => entity.Url)
                .IsRequired()
                .HasMaxLength(2000);
        }
    }
}
