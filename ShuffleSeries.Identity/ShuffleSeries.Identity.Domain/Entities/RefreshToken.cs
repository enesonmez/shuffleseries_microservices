using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.Identity.Domain.Entities;

public class RefreshToken : BaseEntity<Guid>
{
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }
    public string? CreatedByIp { get; private set; }

    public bool IsExpiredAt(DateTime utcNow) => utcNow >= ExpiresAtUtc;
    public bool IsExpired => IsExpiredAt(DateTime.UtcNow);
    public bool IsRevoked => RevokedAtUtc.HasValue;
    public bool IsActive => !IsRevoked && !IsExpired;

    private RefreshToken() { }

    public RefreshToken(Guid userId, string tokenHash, DateTime expiresAtUtc, string? createdByIp = null) : base(Guid.Empty)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
        CreatedByIp = createdByIp;
    }

    public void Revoke(string? replacedByTokenHash = null, DateTime? revokedAtUtc = null)
    {
        RevokedAtUtc = revokedAtUtc ?? DateTime.UtcNow;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}
