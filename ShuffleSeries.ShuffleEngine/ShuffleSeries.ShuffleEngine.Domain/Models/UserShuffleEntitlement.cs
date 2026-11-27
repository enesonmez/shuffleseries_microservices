namespace ShuffleSeries.ShuffleEngine.Domain.Models;

public sealed record UserShuffleEntitlement(
    Guid UserId,
    bool IsPremium,
    string? PlanCode,
    DateTime? ExpiresAtUtc,
    long OccurredAtTicks,
    DateTime UpdatedAtUtc)
{
    public bool IsCurrentlyPremium(DateTime utcNow) =>
        IsPremium && (!ExpiresAtUtc.HasValue || ExpiresAtUtc.Value > utcNow);
}
