using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.Identity.Domain.Entities;

public class UserLogin : BaseEntity<Guid>
{
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

    public string Provider { get; private set; } = string.Empty;
    public string ProviderKey { get; private set; } = string.Empty;
    public string? ProviderEmail { get; private set; }
    public string? RefreshToken { get; private set; }
    public DateTime LinkedAtUtc { get; private set; }

    private UserLogin() { }

    public UserLogin(Guid userId, string provider, string providerKey, string? providerEmail = null, string? refreshToken = null, DateTime? linkedAtUtc = null) : base(Guid.Empty)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerKey);

        UserId = userId;
        Provider = provider.Trim();
        ProviderKey = providerKey.Trim();
        ProviderEmail = providerEmail?.Trim().ToLowerInvariant();
        RefreshToken = refreshToken;
        LinkedAtUtc = linkedAtUtc ?? DateTime.UtcNow;
    }

    public void UpdateRefreshToken(string? refreshToken)
    {
        RefreshToken = refreshToken;
    }
}
