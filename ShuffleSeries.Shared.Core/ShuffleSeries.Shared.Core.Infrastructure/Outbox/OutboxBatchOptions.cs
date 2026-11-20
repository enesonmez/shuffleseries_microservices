namespace ShuffleSeries.Shared.Core.Infrastructure.Outbox;

/// <summary>
/// Outbox toplu işleme politikası parametrelerini kapsüller.
/// </summary>
public sealed record OutboxBatchOptions(
    int BatchSize = OutboxBatchProcessor.DefaultBatchSize,
    int MaxRetries = OutboxBatchProcessor.DefaultMaxRetries,
    int RetentionDays = OutboxBatchProcessor.DefaultRetentionDays)
{
    public static readonly OutboxBatchOptions Default = new();
}
