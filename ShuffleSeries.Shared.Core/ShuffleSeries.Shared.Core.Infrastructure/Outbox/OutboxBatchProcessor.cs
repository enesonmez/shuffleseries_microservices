using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShuffleSeries.Shared.Core.Domain.Outbox;
using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.Shared.Core.Infrastructure.Outbox;

/// <summary>
/// Çoklu mikroservis pod'larında mükerrer yayınları önleyen (PostgreSQL FOR UPDATE SKIP LOCKED),
/// zehirli mesajları karantinaya alan ve eski işlenmiş mesajları temizleyen merkezi Outbox işleyicisi.
/// </summary>
public static class OutboxBatchProcessor
{
    public const int DefaultMaxRetries = 3;
    public const int DefaultBatchSize = 20;
    public const int DefaultRetentionDays = 7;

    private static long _lastPurgeTicks;
    private static readonly long _purgeIntervalTicks = TimeSpan.FromHours(1).Ticks;

    public static async Task ProcessBatchAsync(
        DbContext dbContext,
        Assembly domainAssembly,
        TimeProvider timeProvider,
        ILogger logger,
        Func<IDomainEvent, OutboxMessage, CancellationToken, Task<string>> publishDelegate,
        int batchSize = DefaultBatchSize,
        int maxRetries = DefaultMaxRetries,
        int retentionDays = DefaultRetentionDays,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(domainAssembly);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(publishDelegate);

        var outboxSet = dbContext.Set<OutboxMessage>();

        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        List<OutboxMessage> messages;
        if (dbContext.Database.IsRelational())
        {
            messages = await outboxSet
                .FromSqlInterpolated($@"
                    SELECT * FROM ""OutboxMessages""
                    WHERE ""ProcessedOnUtc"" IS NULL AND ""RetryCount"" < {maxRetries}
                    ORDER BY ""OccurredOnUtc""
                    LIMIT {batchSize}
                    FOR UPDATE SKIP LOCKED")
                .ToListAsync(cancellationToken);
        }
        else
        {
            messages = await outboxSet
                .Where(m => m.ProcessedOnUtc == null && m.RetryCount < maxRetries)
                .OrderBy(m => m.OccurredOnUtc)
                .Take(batchSize)
                .ToListAsync(cancellationToken);
        }

        if (messages.Count == 0)
        {
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            await TryPurgeProcessedMessagesAsync(dbContext, timeProvider, retentionDays, logger, cancellationToken);
            return;
        }

        foreach (var outboxMessage in messages)
        {
            await ProcessSingleMessageAsync(outboxMessage, domainAssembly, timeProvider, logger, publishDelegate, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        await TryPurgeProcessedMessagesAsync(dbContext, timeProvider, retentionDays, logger, cancellationToken);
    }

    private static async Task ProcessSingleMessageAsync(
        OutboxMessage outboxMessage,
        Assembly domainAssembly,
        TimeProvider timeProvider,
        ILogger logger,
        Func<IDomainEvent, OutboxMessage, CancellationToken, Task<string>> publishDelegate,
        CancellationToken cancellationToken)
    {
        try
        {
            var eventType = DomainEventTypeCache.Resolve(domainAssembly, outboxMessage.Type);
            if (eventType is null)
            {
                outboxMessage.Error = $"Not found event type: {outboxMessage.Type}";
                outboxMessage.RetryCount++;
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning("Event type {MessageType} not found for outbox message {MessageId}. Retry: {RetryCount}",
                        outboxMessage.Type, outboxMessage.Id, outboxMessage.RetryCount);
                }
                return;
            }

            if (JsonSerializer.Deserialize(outboxMessage.Content, eventType) is not IDomainEvent domainEvent)
            {
                outboxMessage.Error = "The message content could not be deserialized.";
                outboxMessage.RetryCount++;
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning("Message content could not be deserialized for outbox message {MessageId}. Retry: {RetryCount}",
                        outboxMessage.Id, outboxMessage.RetryCount);
                }
                return;
            }

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(10));
            var publishTypeName = await publishDelegate(domainEvent, outboxMessage, timeoutCts.Token);

            outboxMessage.ProcessedOnUtc = timeProvider.GetUtcNow().UtcDateTime;
            outboxMessage.Error = null;

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "Outbox message {MessageId} of type {MessageType} published successfully as {PublishType}.",
                    outboxMessage.Id,
                    outboxMessage.Type,
                    publishTypeName);
            }
        }
        catch (Exception ex)
        {
            outboxMessage.Error = ex.Message;
            outboxMessage.RetryCount++;

            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(
                    ex,
                    "Failed to process outbox message {MessageId} of type {MessageType}. Retry: {RetryCount}",
                    outboxMessage.Id,
                    outboxMessage.Type,
                    outboxMessage.RetryCount);
            }
        }
    }

    private static async Task TryPurgeProcessedMessagesAsync(
        DbContext dbContext,
        TimeProvider timeProvider,
        int retentionDays,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var nowTicks = timeProvider.GetUtcNow().UtcTicks;
        var lastTicks = Volatile.Read(ref _lastPurgeTicks);
        if (nowTicks - lastTicks < _purgeIntervalTicks)
        {
            return;
        }

        if (Interlocked.CompareExchange(ref _lastPurgeTicks, nowTicks, lastTicks) != lastTicks)
        {
            return;
        }

        await PurgeProcessedMessagesAsync(dbContext, timeProvider, retentionDays, logger, cancellationToken);
    }

    private static async Task PurgeProcessedMessagesAsync(
        DbContext dbContext,
        TimeProvider timeProvider,
        int retentionDays,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (!dbContext.Database.IsRelational() || retentionDays <= 0)
        {
            return;
        }

        try
        {
            var cutoffUtc = timeProvider.GetUtcNow().AddDays(-retentionDays).UtcDateTime;
            var deletedCount = await dbContext.Set<OutboxMessage>()
                .Where(m => m.ProcessedOnUtc != null && m.ProcessedOnUtc < cutoffUtc)
                .ExecuteDeleteAsync(cancellationToken);

            if (deletedCount > 0 && logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Purged {DeletedCount} processed outbox messages older than {CutoffUtc}.", deletedCount, cutoffUtc);
            }
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(ex, "Failed to purge old processed outbox messages.");
            }
        }
    }
}
