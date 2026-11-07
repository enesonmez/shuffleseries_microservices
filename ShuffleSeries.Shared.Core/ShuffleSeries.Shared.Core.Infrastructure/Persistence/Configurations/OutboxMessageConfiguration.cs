using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShuffleSeries.Shared.Core.Domain.Outbox;

namespace ShuffleSeries.Shared.Core.Infrastructure.Persistence.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type).IsRequired().HasMaxLength(255);
        builder.Property(x => x.Content).IsRequired().HasColumnType("jsonb");
        builder.Property(x => x.RetryCount).HasDefaultValue(0);

        builder.HasIndex(x => x.OccurredOnUtc)
            .HasDatabaseName("IX_OutboxMessages_Pending")
            .HasFilter("\"ProcessedOnUtc\" IS NULL AND \"RetryCount\" < 3");

        builder.HasIndex(x => x.ProcessedOnUtc)
            .HasDatabaseName("IX_OutboxMessages_Processed")
            .HasFilter("\"ProcessedOnUtc\" IS NOT NULL");
    }
}
