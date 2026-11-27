using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShuffleSeries.Shared.Core.Domain.Inbox;

namespace ShuffleSeries.Shared.Core.Infrastructure.Persistence.Configurations;

public sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("InboxMessages");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.EventType).IsRequired().HasMaxLength(255);
        builder.Property(x => x.Content).IsRequired().HasColumnType("jsonb");
        builder.Property(x => x.CorrelationId).HasMaxLength(128);
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        builder.HasIndex(x => x.ProcessedAtUtc)
            .HasDatabaseName("IX_InboxMessages_Processed")
            .HasFilter("\"ProcessedAtUtc\" IS NOT NULL");
    }
}
