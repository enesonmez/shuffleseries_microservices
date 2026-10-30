using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShuffleSeries.Catalog.Domain.Entities;

namespace ShuffleSeries.Catalog.Infrastructure.Persistence.Configurations;

public class MediaGenreConfiguration : IEntityTypeConfiguration<MediaGenre>
{
    public void Configure(EntityTypeBuilder<MediaGenre> builder)
    {
        builder.ToTable("MediaGenres");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.MediaId, x.GenreId })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        // Ters FK indeksi (docs/lessons.md satır 24 kuralı: Junction tablolarında reverse FK indexing)
        builder.HasIndex(x => x.GenreId);

        builder.HasOne(x => x.Genre)
            .WithMany()
            .HasForeignKey(x => x.GenreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);
    }
}
