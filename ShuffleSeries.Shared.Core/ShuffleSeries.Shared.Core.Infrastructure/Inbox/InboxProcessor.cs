using System.Runtime.ExceptionServices;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShuffleSeries.Shared.Core.Domain.Inbox;
using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.Shared.Core.Infrastructure.Inbox;

/// <summary>
/// Provides race-free, multi-pod Transactional Inbox idempotency for integration event consumers
/// using atomic PostgreSQL Claim-Execute-Seal (zero DB connections held during external I/O)
/// and throttled retention purge.
/// </summary>
public static class InboxProcessor
{
    public const int DefaultRetentionDays = 7;
    public static readonly TimeSpan DefaultStaleClaimTimeout = TimeSpan.FromMinutes(2);

    private static long _lastPurgeTicks;
    private static readonly long _purgeIntervalTicks = TimeSpan.FromHours(1).Ticks;

    public static async Task<bool> ExecuteIdempotentAsync<TEvent>(
        DbContext dbContext,
        TEvent integrationEvent,
        TimeProvider timeProvider,
        ILogger logger,
        Func<CancellationToken, Task> handlerAction,
        InboxOptions? options = null,
        CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(integrationEvent);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(handlerAction);

        var effectiveOptions = options ?? InboxOptions.Default;
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var payloadJson = JsonSerializer.Serialize(integrationEvent);

        var invocation = new InboxInvocation<TEvent>(
            dbContext,
            integrationEvent,
            payloadJson,
            nowUtc,
            timeProvider,
            logger,
            handlerAction,
            effectiveOptions);

        return dbContext.Database.IsRelational()
            ? await ExecuteRelationalIdempotentAsync(invocation, cancellationToken)
            : await ExecuteNonRelationalIdempotentAsync(invocation, cancellationToken);
    }

    private readonly record struct InboxInvocation<TEvent>(
        DbContext DbContext,
        TEvent IntegrationEvent,
        string PayloadJson,
        DateTime NowUtc,
        TimeProvider TimeProvider,
        ILogger Logger,
        Func<CancellationToken, Task> HandlerAction,
        InboxOptions Options)
        where TEvent : IIntegrationEvent;

    private static async Task<bool> ExecuteRelationalIdempotentAsync<TEvent>(
        InboxInvocation<TEvent> invocation,
        CancellationToken cancellationToken)
        where TEvent : IIntegrationEvent
    {
        var dbContext = invocation.DbContext;
        var integrationEvent = invocation.IntegrationEvent;
        var nowUtc = invocation.NowUtc;
        var logger = invocation.Logger;

        // Phase 1 (Atomic Claim - <1ms, connection released immediately before external I/O):
        // Only a single pod can win INSERT; retries reclaim rows with Error IS NOT NULL or expired crash leases.
        var insertedRows = await dbContext.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO ""InboxMessages"" (
                ""Id"", ""EventType"", ""Content"", ""OccurredOnUtc"",
                ""CorrelationId"", ""ReceivedAtUtc"", ""CreatedAtUtc"", ""ModifiedAtUtc"", ""IsDeleted"")
            VALUES (
                {integrationEvent.Id}, {integrationEvent.EventType}, {invocation.PayloadJson}::jsonb, {integrationEvent.OccurredOnUtc},
                {integrationEvent.CorrelationId}, {nowUtc}, {nowUtc}, {nowUtc}, false)
            ON CONFLICT (""Id"") DO NOTHING;",
            cancellationToken);

        var claimed = insertedRows > 0;
        if (!claimed)
        {
            var staleClaimCutoffUtc = nowUtc.Subtract(invocation.Options.EffectiveStaleClaimTimeout);
            var reclaimedRows = await dbContext.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE ""InboxMessages""
                SET ""Error"" = NULL,
                    ""ModifiedAtUtc"" = {nowUtc}
                WHERE ""Id"" = {integrationEvent.Id}
                  AND ""ProcessedAtUtc"" IS NULL
                  AND (""Error"" IS NOT NULL OR COALESCE(""ModifiedAtUtc"", ""ReceivedAtUtc"") < {staleClaimCutoffUtc});",
                cancellationToken);

            claimed = reclaimedRows > 0;
        }

        if (!claimed)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "Duplicate or in-flight integration event {EventId} ({EventType}) skipped via Inbox idempotency guard.",
                    integrationEvent.Id,
                    integrationEvent.EventType);
            }

            return false;
        }

        // Phase 2 (External I/O Execution - ZERO PostgreSQL transactions or locks held!)
        try
        {
            await invocation.HandlerAction(cancellationToken);

            // Phase 3a (Atomic Seal - mark processed)
            var completedAtUtc = invocation.TimeProvider.GetUtcNow().UtcDateTime;
            await dbContext.Set<InboxMessage>()
                .IgnoreQueryFilters()
                .Where(m => m.Id == integrationEvent.Id)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(m => m.ProcessedAtUtc, completedAtUtc)
                        .SetProperty(m => m.Error, (string?)null)
                        .SetProperty(m => m.ModifiedAtUtc, completedAtUtc),
                    cancellationToken);
        }
        catch (Exception ex)
        {
            // Phase 3b (25P02-safe Error Recording - isolated so original exception is never masked)
            await RecordFailureSafelyAsync(dbContext, integrationEvent.Id, ex.Message, invocation.TimeProvider, logger);

            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(
                    ex,
                    "Failed processing integration event {EventId} ({EventType}); recorded error in Inbox for retry.",
                    integrationEvent.Id,
                    integrationEvent.EventType);
            }

            ExceptionDispatchInfo.Capture(ex).Throw();
        }

        await TryPurgeProcessedMessagesAsync(dbContext, invocation.TimeProvider, invocation.Options.RetentionDays, logger, cancellationToken);
        return true;
    }

    private static async Task<bool> ExecuteNonRelationalIdempotentAsync<TEvent>(
        InboxInvocation<TEvent> invocation,
        CancellationToken cancellationToken)
        where TEvent : IIntegrationEvent
    {
        var dbContext = invocation.DbContext;
        var integrationEvent = invocation.IntegrationEvent;
        var logger = invocation.Logger;

        var existing = await dbContext.Set<InboxMessage>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == integrationEvent.Id, cancellationToken);

        if (existing is null)
        {
            existing = new InboxMessage(
                integrationEvent.Id,
                integrationEvent.EventType,
                invocation.PayloadJson,
                integrationEvent.OccurredOnUtc,
                integrationEvent.CorrelationId,
                invocation.NowUtc);
            await dbContext.Set<InboxMessage>().AddAsync(existing, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (existing.IsProcessed)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "Duplicate integration event {EventId} ({EventType}) skipped via Inbox idempotency guard.",
                    integrationEvent.Id,
                    integrationEvent.EventType);
            }

            return false;
        }

        try
        {
            await invocation.HandlerAction(cancellationToken);

            existing.MarkAsProcessed(invocation.TimeProvider.GetUtcNow().UtcDateTime);
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            existing.MarkAsFailed(ex.Message);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(
                    ex,
                    "Failed processing integration event {EventId} ({EventType}); recorded error in Inbox for retry.",
                    integrationEvent.Id,
                    integrationEvent.EventType);
            }

            ExceptionDispatchInfo.Capture(ex).Throw();
            return false;
        }
    }

    private static async Task RecordFailureSafelyAsync(
        DbContext dbContext,
        Guid eventId,
        string errorMessage,
        TimeProvider timeProvider,
        ILogger logger)
    {
        try
        {
            if (dbContext.Database.CurrentTransaction is not null)
            {
                await dbContext.Database.RollbackTransactionAsync(CancellationToken.None);
            }

            dbContext.ChangeTracker.Clear();

            var failedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
            await dbContext.Set<InboxMessage>()
                .IgnoreQueryFilters()
                .Where(m => m.Id == eventId && m.ProcessedAtUtc == null)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(m => m.Error, errorMessage)
                        .SetProperty(m => m.ModifiedAtUtc, failedAtUtc),
                    CancellationToken.None);
        }
        catch (Exception recordEx)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(recordEx, "Failed to persist Inbox error state for EventId={EventId}.", eventId);
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

    public static async Task PurgeProcessedMessagesAsync(
        DbContext dbContext,
        TimeProvider timeProvider,
        int retentionDays,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (!dbContext.Database.IsRelational() || retentionDays <= 0)
        {
            return;
        }

        try
        {
            var cutoffUtc = timeProvider.GetUtcNow().AddDays(-retentionDays).UtcDateTime;
            var deletedCount = await dbContext.Set<InboxMessage>()
                .IgnoreQueryFilters()
                .Where(m => m.ProcessedAtUtc != null && m.ProcessedAtUtc < cutoffUtc)
                .ExecuteDeleteAsync(cancellationToken);

            if (deletedCount > 0 && logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Purged {DeletedCount} processed inbox messages older than {CutoffUtc}.", deletedCount, cutoffUtc);
            }
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(ex, "Failed to purge old processed inbox messages.");
            }
        }
    }
}
