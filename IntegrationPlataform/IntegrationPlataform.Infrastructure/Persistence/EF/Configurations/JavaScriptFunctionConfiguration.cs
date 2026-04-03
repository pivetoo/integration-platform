using IntegrationPlataform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntegrationPlataform.Infrastructure.Persistence.EF.Configurations
{
    public sealed class JavaScriptFunctionConfiguration : IEntityTypeConfiguration<JavaScriptFunction>
    {
        public void Configure(EntityTypeBuilder<JavaScriptFunction> builder)
        {
            builder.ToTable("javascriptfunction");

            builder.Property(entity => entity.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(entity => entity.Description)
                .HasMaxLength(500);

            builder.Property(entity => entity.Code)
                .IsRequired();
        }
    }
}
