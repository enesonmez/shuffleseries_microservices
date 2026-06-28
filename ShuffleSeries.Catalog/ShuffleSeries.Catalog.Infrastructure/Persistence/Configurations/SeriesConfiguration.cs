using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShuffleSeries.Catalog.Domain.Entities;

namespace ShuffleSeries.Catalog.Infrastructure.Persistence.Configurations;

public class SeriesConfiguration : IEntityTypeConfiguration<Series>
{
    public void Configure(EntityTypeBuilder<Series> builder)
    {
        builder.ToTable("Series");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Title).IsRequired().HasMaxLength(150);
        
        builder.HasIndex(x => x.Title).IsUnique();
        
        builder.Property(x => x.Description).IsRequired().HasMaxLength(1000);
    }
}