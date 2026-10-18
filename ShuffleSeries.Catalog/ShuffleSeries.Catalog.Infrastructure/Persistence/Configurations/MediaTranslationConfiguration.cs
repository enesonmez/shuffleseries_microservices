using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Shared.Core.Domain.ValueObjects;

namespace ShuffleSeries.Catalog.Infrastructure.Persistence.Configurations;

public class MediaTranslationConfiguration : IEntityTypeConfiguration<MediaTranslation>
{
    public void Configure(EntityTypeBuilder<MediaTranslation> builder)
    {
        builder.ToTable("MediaTranslations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.LanguageCode)
            .HasConversion(
                x => x.Value,
                v => LanguageCode.From(v))
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(x => x.Overview)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(x => x.Tagline)
            .HasMaxLength(500);

        builder.HasIndex(x => new { x.MediaId, x.LanguageCode })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(x => x.LanguageCode);
        builder.HasIndex(x => x.Title);

        builder.Property(x => x.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasIndex(x => x.IsDeleted);
    }
}
