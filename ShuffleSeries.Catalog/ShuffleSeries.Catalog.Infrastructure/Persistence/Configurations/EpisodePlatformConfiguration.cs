using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShuffleSeries.Catalog.Domain.Entities;

namespace ShuffleSeries.Catalog.Infrastructure.Persistence.Configurations;

public class EpisodePlatformConfiguration : IEntityTypeConfiguration<EpisodePlatform>
{
    public void Configure(EntityTypeBuilder<EpisodePlatform> builder)
    {
        builder.ToTable("EpisodePlatforms");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.WebUrl)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(x => x.AppSchemeUrl)
            .HasMaxLength(500);

        builder.HasIndex(x => new { x.EpisodeId, x.PlatformId })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        // Ters FK indeksi
        builder.HasIndex(x => x.PlatformId);

        builder.HasOne(x => x.Platform)
            .WithMany()
            .HasForeignKey(x => x.PlatformId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);
    }
}
