using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShuffleSeries.Catalog.Domain.Entities;

namespace ShuffleSeries.Catalog.Infrastructure.Persistence.Configurations;

public class EpisodeConfiguration : IEntityTypeConfiguration<Episode>
{
    public void Configure(EntityTypeBuilder<Episode> builder)
    {
        builder.ToTable("Episodes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.EpisodeNumber)
            .IsRequired();

        builder.Property(x => x.IsStandalone)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.ThumbnailUrl)
            .HasMaxLength(500);

        builder.Property(x => x.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasIndex(x => new { x.SeasonId, x.EpisodeNumber })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        // Bölüm Ruleti için ultra optimize kısmi B-Tree indeks (ImdbRating sıralamasını kapsar)
        builder.HasIndex(x => x.ImdbRating)
            .HasFilter("\"IsStandalone\" = true AND \"IsDeleted\" = false");

        builder.HasMany(x => x.Translations)
            .WithOne()
            .HasForeignKey(x => x.EpisodeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.EpisodePlatforms)
            .WithOne()
            .HasForeignKey(x => x.EpisodeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
