using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShuffleSeries.Catalog.Domain.Entities;

namespace ShuffleSeries.Catalog.Infrastructure.Persistence.Configurations;

public class SeriesConfiguration : IEntityTypeConfiguration<Series>
{
    public void Configure(EntityTypeBuilder<Series> builder)
    {
        builder.Property(x => x.SeriesType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.TotalSeasons)
            .IsRequired()
            .HasDefaultValue(0);

        builder.HasMany(x => x.Seasons)
            .WithOne()
            .HasForeignKey(x => x.SeriesId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
