using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Shared.Core.Domain.ValueObjects;

namespace ShuffleSeries.Catalog.Infrastructure.Persistence.Configurations;

public class EpisodeTranslationConfiguration : IEntityTypeConfiguration<EpisodeTranslation>
{
    public void Configure(EntityTypeBuilder<EpisodeTranslation> builder)
    {
        builder.ToTable("EpisodeTranslations");

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
            .HasMaxLength(2000);

        builder.HasIndex(x => new { x.EpisodeId, x.LanguageCode })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.Property(x => x.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasIndex(x => x.IsDeleted);
    }
}
