using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Enums;

namespace ShuffleSeries.Catalog.Infrastructure.Persistence.Configurations;

public class MediaConfiguration : IEntityTypeConfiguration<Media>
{
    public void Configure(EntityTypeBuilder<Media> builder)
    {
        builder.ToTable("Media");

        builder.HasKey(x => x.Id);

        builder.HasDiscriminator(x => x.MediaType)
            .HasValue<Movie>(MediaType.Movie)
            .HasValue<Series>(MediaType.Series);

        builder.Property(x => x.ImdbId)
            .HasMaxLength(20);

        builder.HasIndex(x => x.ImdbId)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false AND \"ImdbId\" IS NOT NULL");

        builder.Property(x => x.PosterUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.BackdropUrl)
            .HasMaxLength(500);

        builder.Property(x => x.ReleaseYear)
            .IsRequired();

        builder.HasIndex(x => x.ReleaseYear);

        builder.Property(x => x.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasIndex(x => x.IsDeleted);

        builder.HasIndex(x => new { x.MediaType, x.CreatedAtUtc })
            .HasFilter("\"IsDeleted\" = false");

        builder.HasMany(x => x.Translations)
            .WithOne()
            .HasForeignKey(x => x.MediaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.MediaPlatforms)
            .WithOne()
            .HasForeignKey(x => x.MediaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.MediaMoods)
            .WithOne()
            .HasForeignKey(x => x.MediaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.MediaGenres)
            .WithOne()
            .HasForeignKey(x => x.MediaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
