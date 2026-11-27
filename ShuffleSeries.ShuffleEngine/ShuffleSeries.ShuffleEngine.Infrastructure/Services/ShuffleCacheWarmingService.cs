using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShuffleSeries.Shared.Core.Application.Requests;
using ShuffleSeries.Shared.Core.Infrastructure.Configuration.Redis;
using ShuffleSeries.ShuffleEngine.Application.Common.Interfaces;
using ShuffleSeries.ShuffleEngine.Application.Common.Models;
using ShuffleSeries.ShuffleEngine.Application.Features.CacheSync.Responses;
using ShuffleSeries.ShuffleEngine.Domain.Enums;
using ShuffleSeries.ShuffleEngine.Infrastructure.Constants;
using StackExchange.Redis;

namespace ShuffleSeries.ShuffleEngine.Infrastructure.Services;

internal sealed class ShuffleCacheWarmingService(
    IShufflePoolStore poolStore,
    ICatalogSyncGrpcClient catalogClient,
    IConnectionMultiplexer redis,
    IOptions<RedisOptions> redisOptions,
    TimeProvider timeProvider,
    ILogger<ShuffleCacheWarmingService> logger) : IShuffleCacheWarmingService
{
    private const int SyncPageSize = PaginationRequest.MaxPageSize; // 100

    private static readonly SemaphoreSlim _localWarmLock = new(1, 1);

    private string Prefix => redisOptions.Value.InstanceName ?? string.Empty;

    public async Task<WarmShuffleCacheResponse> EnsureWarmedAsync(
        bool forceRebuild = false,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        // Fast-path lock-free check
        if (!forceRebuild && await poolStore.IsPoolWarmedAsync(cancellationToken))
        {
            var currentStats = await poolStore.GetPoolStatisticsAsync(cancellationToken);
            return MapSkippedResponse(currentStats, stopwatch.ElapsedMilliseconds, timeProvider);
        }

        await _localWarmLock.WaitAsync(cancellationToken);
        try
        {
            if (!forceRebuild && await poolStore.IsPoolWarmedAsync(cancellationToken))
            {
                var currentStats = await poolStore.GetPoolStatisticsAsync(cancellationToken);
                return MapSkippedResponse(currentStats, stopwatch.ElapsedMilliseconds, timeProvider);
            }

            var db = redis.GetDatabase();
            var lockKey = (RedisKey)ShuffleRedisKeys.WarmDistributedLock(Prefix);
            var lockToken = Guid.NewGuid().ToString("N");

            var acquired = await db.LockTakeAsync(lockKey, lockToken, ShuffleRedisKeys.WarmLockLease);
            if (!acquired)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Another ShuffleEngine replica is currently warming the Redis pool; skipping duplicate run.");
                }

                var currentStats = await poolStore.GetPoolStatisticsAsync(cancellationToken);
                return MapSkippedResponse(currentStats, stopwatch.ElapsedMilliseconds, timeProvider);
            }

            try
            {
                if (forceRebuild)
                {
                    await poolStore.ClearWarmMarkerAsync(cancellationToken);
                }

                var syncTicks = timeProvider.GetUtcNow().UtcTicks;
                var (moviesSynced, seriesSynced, mediaSafetyNet) = await SyncAllMediaPagesAsync(syncTicks, cancellationToken);
                var (episodesSynced, episodesSafetyNet) = await SyncAllStandaloneEpisodePagesAsync(syncTicks, cancellationToken);

                if (forceRebuild)
                {
                    await poolStore.PruneStaleEntriesAsync(syncTicks, cancellationToken);
                }

                var warmedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
                var totalSafetyNet = mediaSafetyNet + episodesSafetyNet;

                await poolStore.MarkPoolWarmedAsync(
                    new ShufflePoolWarmState(moviesSynced, seriesSynced, episodesSynced, totalSafetyNet, warmedAtUtc),
                    cancellationToken);

                stopwatch.Stop();

                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation(
                        "ShuffleEngine Redis Cache Warming completed in {DurationMs}ms: {Movies} movies, {Series} series, {Episodes} standalone episodes ({SafetyNet} safety-net masterpieces).",
                        stopwatch.ElapsedMilliseconds,
                        moviesSynced,
                        seriesSynced,
                        episodesSynced,
                        totalSafetyNet);
                }

                return new WarmShuffleCacheResponse(
                    WasSkipped: false,
                    MoviesSynced: moviesSynced,
                    SeriesSynced: seriesSynced,
                    StandaloneEpisodesSynced: episodesSynced,
                    SafetyNetItemsCount: totalSafetyNet,
                    DurationMs: stopwatch.ElapsedMilliseconds,
                    WarmedAtUtc: warmedAtUtc);
            }
            finally
            {
                await ReleaseDistributedLockSafelyAsync(db, lockKey, lockToken);
            }
        }
        finally
        {
            _localWarmLock.Release();
        }
    }

    private async Task<(int Movies, int Series, int SafetyNet)> SyncAllMediaPagesAsync(
        long syncTicks,
        CancellationToken cancellationToken)
    {
        var page = 1;
        var moviesCount = 0;
        var seriesCount = 0;
        var safetyNetCount = 0;

        while (true)
        {
            var result = await catalogClient.GetBulkMediaPageAsync(page, SyncPageSize, cancellationToken);
            if (result.Items.Count == 0)
            {
                break;
            }

            await poolStore.UpsertMediaBatchAsync(result.Items, syncTicks, cancellationToken);

            for (var i = 0; i < result.Items.Count; i++)
            {
                var item = result.Items[i];
                if (item.Kind == ShuffleMediaKind.Movie)
                    moviesCount++;
                else if (item.Kind == ShuffleMediaKind.Series)
                    seriesCount++;

                if (item.IsSafetyNetEligible)
                    safetyNetCount++;
            }

            if (page * SyncPageSize >= result.TotalCount)
            {
                break;
            }

            page++;
        }

        return (moviesCount, seriesCount, safetyNetCount);
    }

    private async Task<(int Episodes, int SafetyNet)> SyncAllStandaloneEpisodePagesAsync(
        long syncTicks,
        CancellationToken cancellationToken)
    {
        var page = 1;
        var episodesCount = 0;
        var safetyNetCount = 0;

        while (true)
        {
            var result = await catalogClient.GetStandaloneEpisodesPageAsync(page, SyncPageSize, cancellationToken);
            if (result.Items.Count == 0)
            {
                break;
            }

            await poolStore.UpsertEpisodesBatchAsync(result.Items, syncTicks, cancellationToken);

            for (var i = 0; i < result.Items.Count; i++)
            {
                episodesCount++;
                if (result.Items[i].IsSafetyNetEligible)
                {
                    safetyNetCount++;
                }
            }

            if (page * SyncPageSize >= result.TotalCount)
            {
                break;
            }

            page++;
        }

        return (episodesCount, safetyNetCount);
    }

    private async Task ReleaseDistributedLockSafelyAsync(IDatabase db, RedisKey lockKey, string lockToken)
    {
        try
        {
            await db.LockReleaseAsync(lockKey, lockToken);
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(ex, "Failed to release Redis warming lock {LockKey}; lease will auto-expire.", lockKey.ToString());
            }
        }
    }

    private static WarmShuffleCacheResponse MapSkippedResponse(
        ShufflePoolStatistics stats,
        long durationMs,
        TimeProvider timeProvider) =>
        new(
            WasSkipped: true,
            MoviesSynced: stats.MoviesCount,
            SeriesSynced: stats.SeriesCount,
            StandaloneEpisodesSynced: stats.StandaloneEpisodesCount,
            SafetyNetItemsCount: stats.SafetyNetMoviesCount + stats.SafetyNetSeriesCount + stats.SafetyNetEpisodesCount,
            DurationMs: durationMs,
            WarmedAtUtc: stats.LastWarmedAtUtc ?? timeProvider.GetUtcNow().UtcDateTime);
}
