using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShuffleSeries.Catalog.Domain.Entities;

namespace ShuffleSeries.Catalog.Infrastructure.Persistence.Configurations;

public class SeasonConfiguration : IEntityTypeConfiguration<Season>
{
    public void Configure(EntityTypeBuilder<Season> builder)
    {
        builder.ToTable("Seasons");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SeasonNumber)
            .IsRequired();

        builder.Property(x => x.Title)
            .HasMaxLength(150);

        builder.Property(x => x.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasIndex(x => x.IsDeleted);

        builder.HasIndex(x => new { x.SeriesId, x.SeasonNumber })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasMany(x => x.Episodes)
            .WithOne()
            .HasForeignKey(x => x.SeasonId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
