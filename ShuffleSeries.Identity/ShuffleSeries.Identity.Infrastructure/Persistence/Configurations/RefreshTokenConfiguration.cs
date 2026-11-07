using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShuffleSeries.Identity.Domain.Entities;

namespace ShuffleSeries.Identity.Infrastructure.Persistence.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(rt => rt.Id);

        builder.Property<uint>("xmin").IsRowVersion();

        builder.Property(rt => rt.TokenHash)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(rt => rt.TokenHash)
            .IsUnique();

        builder.HasIndex(rt => rt.UserId);

        builder.HasIndex(rt => rt.ExpiresAtUtc);

        builder.HasIndex(rt => rt.ReplacedByTokenHash)
            .HasFilter("\"ReplacedByTokenHash\" IS NOT NULL");

        builder.Property(rt => rt.ExpiresAtUtc)
            .IsRequired();

        builder.Property(rt => rt.ReplacedByTokenHash)
            .HasMaxLength(256);

        builder.Property(rt => rt.CreatedByIp)
            .HasMaxLength(64);

        builder.HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
