using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShuffleSeries.Identity.Domain.Entities;

namespace ShuffleSeries.Identity.Infrastructure.Persistence.Configurations;

internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Code)
            .IsRequired()
            .HasMaxLength(128);

        builder.HasIndex(p => p.Code)
            .IsUnique();

        builder.Property(p => p.Group)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(p => p.Description)
            .HasMaxLength(256);
    }
}
