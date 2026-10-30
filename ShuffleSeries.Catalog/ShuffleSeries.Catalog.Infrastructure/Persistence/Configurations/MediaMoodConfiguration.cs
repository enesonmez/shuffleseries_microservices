using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShuffleSeries.Catalog.Domain.Entities;

namespace ShuffleSeries.Catalog.Infrastructure.Persistence.Configurations;

public class MediaMoodConfiguration : IEntityTypeConfiguration<MediaMood>
{
    public void Configure(EntityTypeBuilder<MediaMood> builder)
    {
        builder.ToTable("MediaMoods");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.MediaId, x.MoodId })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        // Ters FK indeksi (docs/lessons.md satır 24 kuralı)
        builder.HasIndex(x => x.MoodId);

        builder.HasOne(x => x.Mood)
            .WithMany()
            .HasForeignKey(x => x.MoodId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);
    }
}
