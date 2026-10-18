using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShuffleSeries.Catalog.Domain.Entities;

namespace ShuffleSeries.Catalog.Infrastructure.Persistence.Configurations;

public class MoodConfiguration : IEntityTypeConfiguration<Mood>
{
    public void Configure(EntityTypeBuilder<Mood> builder)
    {
        builder.ToTable("Moods");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.Property(x => x.AmbientColorHex)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.IconUrl)
            .HasMaxLength(500);

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => x.DisplayOrder);

        builder.Property(x => x.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasIndex(x => x.IsDeleted);

        builder.HasMany(x => x.Translations)
            .WithOne()
            .HasForeignKey(x => x.MoodId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
