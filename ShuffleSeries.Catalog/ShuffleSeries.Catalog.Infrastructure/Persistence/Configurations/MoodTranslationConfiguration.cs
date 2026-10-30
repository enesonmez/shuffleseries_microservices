using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Shared.Core.Domain.ValueObjects;

namespace ShuffleSeries.Catalog.Infrastructure.Persistence.Configurations;

public class MoodTranslationConfiguration : IEntityTypeConfiguration<MoodTranslation>
{
    public void Configure(EntityTypeBuilder<MoodTranslation> builder)
    {
        builder.ToTable("MoodTranslations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.LanguageCode)
            .HasConversion(
                x => x.Value,
                v => LanguageCode.From(v))
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.VibeTags)
            .HasMaxLength(500);

        builder.HasIndex(x => new { x.MoodId, x.LanguageCode })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.Property(x => x.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);
    }
}
