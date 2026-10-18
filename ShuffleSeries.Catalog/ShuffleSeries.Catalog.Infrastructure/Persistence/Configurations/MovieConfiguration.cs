using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShuffleSeries.Catalog.Domain.Entities;

namespace ShuffleSeries.Catalog.Infrastructure.Persistence.Configurations;

public class MovieConfiguration : IEntityTypeConfiguration<Movie>
{
    public void Configure(EntityTypeBuilder<Movie> builder)
    {
        builder.Property(x => x.RuntimeMinutes)
            .IsRequired();

        builder.Property(x => x.Director)
            .HasMaxLength(150);
    }
}
