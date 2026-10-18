using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShuffleSeries.Catalog.Domain.Entities;

namespace ShuffleSeries.Catalog.Infrastructure.Persistence.Configurations;

public class PlatformConfiguration : IEntityTypeConfiguration<Platform>
{
    public void Configure(EntityTypeBuilder<Platform> builder)
    {
        builder.ToTable("Platforms");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Slug)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(x => x.Slug)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.Property(x => x.LogoUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.InactiveLogoUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.BaseWebUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.BaseAppSchemeUrl)
            .HasMaxLength(250);

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => x.DisplayOrder);

        builder.Property(x => x.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasIndex(x => x.IsDeleted);
    }
}
