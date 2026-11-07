using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShuffleSeries.Identity.Domain.Entities;

namespace ShuffleSeries.Identity.Infrastructure.Persistence.Configurations;

internal sealed class UserLoginConfiguration : IEntityTypeConfiguration<UserLogin>
{
    public void Configure(EntityTypeBuilder<UserLogin> builder)
    {
        builder.ToTable("UserLogins");

        builder.HasKey(ul => ul.Id);

        builder.Property(ul => ul.Provider)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(ul => ul.ProviderKey)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(ul => new { ul.Provider, ul.ProviderKey })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(ul => ul.UserId);

        builder.Property(ul => ul.ProviderEmail)
            .HasMaxLength(256);

        builder.Property(ul => ul.LinkedAtUtc)
            .IsRequired();

        builder.HasOne(ul => ul.User)
            .WithMany(u => u.UserLogins)
            .HasForeignKey(ul => ul.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
