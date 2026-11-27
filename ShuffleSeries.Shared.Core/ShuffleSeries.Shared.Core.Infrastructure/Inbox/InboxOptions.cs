namespace ShuffleSeries.Shared.Core.Infrastructure.Inbox;

public sealed record InboxOptions(
    int RetentionDays = InboxProcessor.DefaultRetentionDays,
    TimeSpan? StaleClaimTimeout = null)
{
    public static readonly InboxOptions Default = new();

    public TimeSpan EffectiveStaleClaimTimeout =>
        StaleClaimTimeout ?? InboxProcessor.DefaultStaleClaimTimeout;
}
