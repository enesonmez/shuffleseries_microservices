using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ShuffleSeries.Shared.Core.Infrastructure.Configuration.Redis;
using ShuffleSeries.ShuffleEngine.Application.Common.Interfaces;
using ShuffleSeries.ShuffleEngine.Application.Common.Models;
using ShuffleSeries.ShuffleEngine.Domain.Enums;
using ShuffleSeries.ShuffleEngine.Domain.Models;
using ShuffleSeries.ShuffleEngine.Infrastructure.Constants;
using StackExchange.Redis;

namespace ShuffleSeries.ShuffleEngine.Infrastructure.Services;

internal sealed class RedisShufflePoolStore(
    IConnectionMultiplexer redis,
    IOptions<RedisOptions> redisOptions) : IShufflePoolStore
{
    private const string FieldPlatformIds = "platformIds";
    private const string FieldMoodIds = "moodIds";
    private const string FieldGenreIds = "genreIds";
    private const string FieldPayload = "payload";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly char[] _commaSeparator = [','];

    private static readonly RedisValue[] _mediaMetaFields =
    [
        "occurredAtTicks",
        "kind",
        FieldPlatformIds,
        FieldMoodIds,
        FieldGenreIds,
        "director"
    ];

    private static readonly RedisValue[] _episodeMetaFields =
    [
        "occurredAtTicks",
        "seriesId",
        FieldPlatformIds,
        FieldMoodIds,
        FieldGenreIds
    ];

    private readonly record struct RedisIndexWriteContext(
        IBatch Batch,
        List<Task> Tasks,
        string Prefix,
        string MemberId);

    private string Prefix => redisOptions.Value.InstanceName ?? string.Empty;

    public async Task UpsertMediaBatchAsync(
        IReadOnlyList<ShuffleMediaSnapshot> items,
        long occurredAtTicks,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (items.Count == 0)
        {
            return;
        }

        var db = redis.GetDatabase();
        var prefix = Prefix;

        // Pass 1: Pipelined read of existing metadata to prevent N+1 roundtrips and detect stale/removed tags
        var readBatch = db.CreateBatch();
        var readTasks = new Task<RedisValue[]>[items.Count];
        for (var i = 0; i < items.Count; i++)
        {
            readTasks[i] = readBatch.HashGetAsync(ShuffleRedisKeys.MediaHash(prefix, items[i].Id), _mediaMetaFields);
        }

        readBatch.Execute();
        var existingMetaList = await Task.WhenAll(readTasks);

        // Pass 2: Single pipelined write batch updating hashes, ZSets, Safety Net, and Set indexes atomically
        var writeBatch = db.CreateBatch();
        var writeTasks = new List<Task>(items.Count * 8);

        for (var i = 0; i < items.Count; i++)
        {
            QueueSingleMediaUpsert(writeBatch, writeTasks, prefix, items[i], existingMetaList[i], occurredAtTicks);
        }

        writeBatch.Execute();
        if (writeTasks.Count > 0)
        {
            await Task.WhenAll(writeTasks);
        }
    }

    private static void QueueSingleMediaUpsert(
        IBatch writeBatch,
        List<Task> writeTasks,
        string prefix,
        ShuffleMediaSnapshot item,
        RedisValue[] existing,
        long occurredAtTicks)
    {
        if (IsNewerVersionStored(existing[0], occurredAtTicks))
        {
            return;
        }

        var itemIdStr = item.Id.ToString();
        var ttl = ShuffleRedisKeys.WithJitter(ShuffleRedisKeys.BasePoolTtl);
        var mediaKey = ShuffleRedisKeys.MediaHash(prefix, item.Id);
        var indexCtx = new RedisIndexWriteContext(writeBatch, writeTasks, prefix, itemIdStr);

        CleanStaleMediaMemberships(indexCtx, item, existing);

        var payloadBytes = JsonSerializer.SerializeToUtf8Bytes(item, _jsonOptions);
        HashEntry[] entries =
        [
            new("kind", (int)item.Kind),
            new("imdbRating", item.ImdbRating?.ToString(CultureInfo.InvariantCulture) ?? string.Empty),
            new("imdbVotes", item.ImdbVotes?.ToString(CultureInfo.InvariantCulture) ?? string.Empty),
            new("releaseYear", item.ReleaseYear),
            new("baseScore", item.BaseQualityScore.ToString(CultureInfo.InvariantCulture)),
            new("director", item.Director ?? string.Empty),
            new(FieldPlatformIds, JoinGuids(item.PlatformIds)),
            new(FieldMoodIds, JoinGuids(item.MoodIds)),
            new(FieldGenreIds, JoinGuids(item.GenreIds)),
            new("occurredAtTicks", occurredAtTicks),
            new(FieldPayload, payloadBytes)
        ];

        writeTasks.Add(writeBatch.HashSetAsync(mediaKey, entries));
        writeTasks.Add(writeBatch.KeyExpireAsync(mediaKey, ttl));

        var poolKey = ShuffleRedisKeys.PoolZSet(prefix, item.Kind);
        writeTasks.Add(writeBatch.SortedSetAddAsync(poolKey, itemIdStr, item.BaseQualityScore));
        writeTasks.Add(writeBatch.KeyExpireAsync(poolKey, ttl));

        var safetyKey = ShuffleRedisKeys.SafetyNetZSet(prefix, item.Kind);
        if (item.IsSafetyNetEligible)
        {
            writeTasks.Add(writeBatch.SortedSetAddAsync(safetyKey, itemIdStr, item.BaseQualityScore));
            writeTasks.Add(writeBatch.KeyExpireAsync(safetyKey, ttl));
        }
        else
        {
            writeTasks.Add(writeBatch.SortedSetRemoveAsync(safetyKey, itemIdStr));
        }

        QueueIndexAdditions(indexCtx, item.Kind, item.PlatformIds, ttl, ShuffleRedisKeys.PlatformIndexSet);
        QueueIndexAdditions(indexCtx, item.Kind, item.MoodIds, ttl, ShuffleRedisKeys.MoodIndexSet);
        QueueIndexAdditions(indexCtx, item.Kind, item.GenreIds, ttl, ShuffleRedisKeys.GenreIndexSet);
    }

    private static bool IsNewerVersionStored(RedisValue existingTicksValue, long incomingTicks) =>
        incomingTicks > 0 &&
        existingTicksValue.HasValue &&
        long.TryParse((string?)existingTicksValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var existingTicks) &&
        existingTicks > incomingTicks;

    private static void CleanStaleMediaMemberships(
        RedisIndexWriteContext indexCtx,
        ShuffleMediaSnapshot item,
        RedisValue[] existing)
    {
        if (!existing[1].HasValue ||
            !int.TryParse((string?)existing[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var oldKindInt))
        {
            return;
        }

        var oldKind = (ShuffleMediaKind)oldKindInt;
        if (oldKind != item.Kind)
        {
            indexCtx.Tasks.Add(indexCtx.Batch.SortedSetRemoveAsync(ShuffleRedisKeys.PoolZSet(indexCtx.Prefix, oldKind), indexCtx.MemberId));
            indexCtx.Tasks.Add(indexCtx.Batch.SortedSetRemoveAsync(ShuffleRedisKeys.SafetyNetZSet(indexCtx.Prefix, oldKind), indexCtx.MemberId));
        }

        QueueStaleIndexRemovals(indexCtx, oldKind, item.Kind, existing[2], item.PlatformIds, ShuffleRedisKeys.PlatformIndexSet);
        QueueStaleIndexRemovals(indexCtx, oldKind, item.Kind, existing[3], item.MoodIds, ShuffleRedisKeys.MoodIndexSet);
        QueueStaleIndexRemovals(indexCtx, oldKind, item.Kind, existing[4], item.GenreIds, ShuffleRedisKeys.GenreIndexSet);
    }

    public async Task RemoveMediaAsync(Guid mediaId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var db = redis.GetDatabase();
        var prefix = Prefix;
        var mediaKey = ShuffleRedisKeys.MediaHash(prefix, mediaId);
        var seriesEpisodesKey = ShuffleRedisKeys.SeriesEpisodesSet(prefix, mediaId);

        var readBatch = db.CreateBatch();
        var metaTask = readBatch.HashGetAsync(mediaKey, _mediaMetaFields);
        var episodeIdsTask = readBatch.SetMembersAsync(seriesEpisodesKey);
        readBatch.Execute();
        await Task.WhenAll(metaTask, episodeIdsTask);

        var existing = await metaTask;
        var childEpisodeValues = await episodeIdsTask;

        // If this Series had indexed standalone episodes, read their metadata in a single batch to prevent Ghost Active Episodes
        var childEpisodeMeta = new List<(Guid EpisodeId, RedisValue[] Meta)>();
        if (childEpisodeValues.Length > 0)
        {
            var epReadBatch = db.CreateBatch();
            var epMetaTasks = new List<(Guid Id, Task<RedisValue[]> Task)>(childEpisodeValues.Length);
            foreach (var epVal in childEpisodeValues)
            {
                if (Guid.TryParse((string?)epVal, out var epId))
                {
                    epMetaTasks.Add((epId, epReadBatch.HashGetAsync(ShuffleRedisKeys.EpisodeHash(prefix, epId), _episodeMetaFields)));
                }
            }

            epReadBatch.Execute();
            foreach (var (epId, task) in epMetaTasks)
            {
                childEpisodeMeta.Add((epId, await task));
            }
        }

        var writeBatch = db.CreateBatch();
        var writeTasks = new List<Task>(16);
        var mediaIdStr = mediaId.ToString();

        writeTasks.Add(writeBatch.KeyDeleteAsync(mediaKey));
        writeTasks.Add(writeBatch.KeyDeleteAsync(seriesEpisodesKey));
        writeTasks.Add(writeBatch.SortedSetRemoveAsync(ShuffleRedisKeys.PoolZSet(prefix, ShuffleMediaKind.Movie), mediaIdStr));
        writeTasks.Add(writeBatch.SortedSetRemoveAsync(ShuffleRedisKeys.PoolZSet(prefix, ShuffleMediaKind.Series), mediaIdStr));
        writeTasks.Add(writeBatch.SortedSetRemoveAsync(ShuffleRedisKeys.SafetyNetZSet(prefix, ShuffleMediaKind.Movie), mediaIdStr));
        writeTasks.Add(writeBatch.SortedSetRemoveAsync(ShuffleRedisKeys.SafetyNetZSet(prefix, ShuffleMediaKind.Series), mediaIdStr));

        if (existing[1].HasValue &&
            int.TryParse((string?)existing[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var kindInt))
        {
            var kind = (ShuffleMediaKind)kindInt;
            var removeCtx = new RedisIndexWriteContext(writeBatch, writeTasks, prefix, mediaIdStr);
            QueueAllIndexRemovals(removeCtx, kind, existing[2], ShuffleRedisKeys.PlatformIndexSet);
            QueueAllIndexRemovals(removeCtx, kind, existing[3], ShuffleRedisKeys.MoodIndexSet);
            QueueAllIndexRemovals(removeCtx, kind, existing[4], ShuffleRedisKeys.GenreIndexSet);
        }

        foreach (var (epId, epMeta) in childEpisodeMeta)
        {
            QueueEpisodeDeletion(writeBatch, writeTasks, prefix, epId, epMeta);
        }

        writeBatch.Execute();
        await Task.WhenAll(writeTasks);
    }

    public async Task UpsertEpisodesBatchAsync(
        IReadOnlyList<ShuffleEpisodeSnapshot> items,
        long occurredAtTicks,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (items.Count == 0)
        {
            return;
        }

        var db = redis.GetDatabase();
        var prefix = Prefix;

        var readBatch = db.CreateBatch();
        var readTasks = new Task<RedisValue[]>[items.Count];
        for (var i = 0; i < items.Count; i++)
        {
            readTasks[i] = readBatch.HashGetAsync(ShuffleRedisKeys.EpisodeHash(prefix, items[i].EpisodeId), _episodeMetaFields);
        }

        readBatch.Execute();
        var existingMetaList = await Task.WhenAll(readTasks);

        var writeBatch = db.CreateBatch();
        var writeTasks = new List<Task>(items.Count * 8);

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var existing = existingMetaList[i];

            if (IsNewerVersionStored(existing[0], occurredAtTicks))
            {
                continue;
            }

            var epIdStr = item.EpisodeId.ToString();
            var ttl = ShuffleRedisKeys.WithJitter(ShuffleRedisKeys.BasePoolTtl);
            var epKey = ShuffleRedisKeys.EpisodeHash(prefix, item.EpisodeId);
            var indexCtx = new RedisIndexWriteContext(writeBatch, writeTasks, prefix, epIdStr);

            if (existing[1].HasValue)
            {
                QueueStaleIndexRemovals(indexCtx, ShuffleMediaKind.StandaloneEpisode, ShuffleMediaKind.StandaloneEpisode, existing[2], item.PlatformIds, ShuffleRedisKeys.PlatformIndexSet);
                QueueStaleIndexRemovals(indexCtx, ShuffleMediaKind.StandaloneEpisode, ShuffleMediaKind.StandaloneEpisode, existing[3], item.MoodIds, ShuffleRedisKeys.MoodIndexSet);
                QueueStaleIndexRemovals(indexCtx, ShuffleMediaKind.StandaloneEpisode, ShuffleMediaKind.StandaloneEpisode, existing[4], item.GenreIds, ShuffleRedisKeys.GenreIndexSet);
            }

            var payloadBytes = JsonSerializer.SerializeToUtf8Bytes(item, _jsonOptions);
            HashEntry[] entries =
            [
                new("seriesId", item.SeriesId.ToString()),
                new("seasonNumber", item.SeasonNumber),
                new("episodeNumber", item.EpisodeNumber),
                new("imdbRating", item.ImdbRating?.ToString(CultureInfo.InvariantCulture) ?? string.Empty),
                new("runtimeMinutes", item.RuntimeMinutes?.ToString(CultureInfo.InvariantCulture) ?? string.Empty),
                new("baseScore", item.BaseQualityScore.ToString(CultureInfo.InvariantCulture)),
                new(FieldPlatformIds, JoinGuids(item.PlatformIds)),
                new(FieldMoodIds, JoinGuids(item.MoodIds)),
                new(FieldGenreIds, JoinGuids(item.GenreIds)),
                new("occurredAtTicks", occurredAtTicks),
                new(FieldPayload, payloadBytes)
            ];

            writeTasks.Add(writeBatch.HashSetAsync(epKey, entries));
            writeTasks.Add(writeBatch.KeyExpireAsync(epKey, ttl));

            var poolKey = ShuffleRedisKeys.PoolZSet(prefix, ShuffleMediaKind.StandaloneEpisode);
            writeTasks.Add(writeBatch.SortedSetAddAsync(poolKey, epIdStr, item.BaseQualityScore));
            writeTasks.Add(writeBatch.KeyExpireAsync(poolKey, ttl));

            var safetyKey = ShuffleRedisKeys.SafetyNetZSet(prefix, ShuffleMediaKind.StandaloneEpisode);
            if (item.IsSafetyNetEligible)
            {
                writeTasks.Add(writeBatch.SortedSetAddAsync(safetyKey, epIdStr, item.BaseQualityScore));
                writeTasks.Add(writeBatch.KeyExpireAsync(safetyKey, ttl));
            }
            else
            {
                writeTasks.Add(writeBatch.SortedSetRemoveAsync(safetyKey, epIdStr));
            }

            var seriesEpKey = ShuffleRedisKeys.SeriesEpisodesSet(prefix, item.SeriesId);
            writeTasks.Add(writeBatch.SetAddAsync(seriesEpKey, epIdStr));
            writeTasks.Add(writeBatch.KeyExpireAsync(seriesEpKey, ttl));

            QueueIndexAdditions(indexCtx, ShuffleMediaKind.StandaloneEpisode, item.PlatformIds, ttl, ShuffleRedisKeys.PlatformIndexSet);
            QueueIndexAdditions(indexCtx, ShuffleMediaKind.StandaloneEpisode, item.MoodIds, ttl, ShuffleRedisKeys.MoodIndexSet);
            QueueIndexAdditions(indexCtx, ShuffleMediaKind.StandaloneEpisode, item.GenreIds, ttl, ShuffleRedisKeys.GenreIndexSet);
        }

        writeBatch.Execute();
        if (writeTasks.Count > 0)
        {
            await Task.WhenAll(writeTasks);
        }
    }

    public async Task RemoveEpisodeAsync(Guid episodeId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var db = redis.GetDatabase();
        var prefix = Prefix;
        var epKey = ShuffleRedisKeys.EpisodeHash(prefix, episodeId);

        var existing = await db.HashGetAsync(epKey, _episodeMetaFields);

        var writeBatch = db.CreateBatch();
        var writeTasks = new List<Task>(12);
        QueueEpisodeDeletion(writeBatch, writeTasks, prefix, episodeId, existing);
        writeBatch.Execute();
        await Task.WhenAll(writeTasks);
    }

    public async Task ReconcileSeriesEpisodesAsync(
        Guid seriesId,
        IReadOnlyList<ShuffleEpisodeSnapshot> activeEpisodes,
        long occurredAtTicks,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (activeEpisodes.Count > 0)
        {
            await UpsertEpisodesBatchAsync(activeEpisodes, occurredAtTicks, cancellationToken);
        }

        var db = redis.GetDatabase();
        var prefix = Prefix;
        var seriesEpisodesKey = ShuffleRedisKeys.SeriesEpisodesSet(prefix, seriesId);
        var existingMembers = await db.SetMembersAsync(seriesEpisodesKey);
        if (existingMembers.Length == 0)
        {
            return;
        }

        var activeIds = new HashSet<Guid>(activeEpisodes.Count);
        for (var i = 0; i < activeEpisodes.Count; i++)
        {
            activeIds.Add(activeEpisodes[i].EpisodeId);
        }

        var orphanEpisodeIds = new List<Guid>();
        for (var i = 0; i < existingMembers.Length; i++)
        {
            if (Guid.TryParse((string?)existingMembers[i], out var epId) && !activeIds.Contains(epId))
            {
                orphanEpisodeIds.Add(epId);
            }
        }

        if (orphanEpisodeIds.Count == 0)
        {
            return;
        }

        var epReadBatch = db.CreateBatch();
        var epMetaTasks = new Task<RedisValue[]>[orphanEpisodeIds.Count];
        for (var i = 0; i < orphanEpisodeIds.Count; i++)
        {
            epMetaTasks[i] = epReadBatch.HashGetAsync(
                ShuffleRedisKeys.EpisodeHash(prefix, orphanEpisodeIds[i]),
                _episodeMetaFields);
        }

        epReadBatch.Execute();
        var epMetaList = await Task.WhenAll(epMetaTasks);

        var writeBatch = db.CreateBatch();
        var writeTasks = new List<Task>(orphanEpisodeIds.Count * 8 + 1);
        for (var i = 0; i < orphanEpisodeIds.Count; i++)
        {
            QueueEpisodeDeletion(writeBatch, writeTasks, prefix, orphanEpisodeIds[i], epMetaList[i]);
        }

        if (activeEpisodes.Count == 0)
        {
            writeTasks.Add(writeBatch.KeyDeleteAsync(seriesEpisodesKey));
        }

        writeBatch.Execute();
        await Task.WhenAll(writeTasks);
    }

    public async Task PruneStaleEntriesAsync(
        long minOccurredAtTicks,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var db = redis.GetDatabase();
        var prefix = Prefix;

        // Pass 1: Read all current pool ZSET members in one pipeline
        var (mediaCandidates, episodeCandidates) = await ReadPoolCandidatesAsync(db, prefix);
        if (mediaCandidates.Count == 0 && episodeCandidates.Count == 0)
        {
            return;
        }

        // Pass 2: Read metadata of every candidate in one pipeline (Zero Loop I/O)
        var (mediaMetas, episodeMetas) = await ReadCandidateMetasAsync(db, prefix, mediaCandidates, episodeCandidates);

        // Pass 3: Queue atomic removal for any item not refreshed in the current sync cycle
        var writeBatch = db.CreateBatch();
        var writeTasks = new List<Task>();

        QueueStaleMediaPrunes(writeBatch, writeTasks, prefix, mediaCandidates, mediaMetas, minOccurredAtTicks);
        QueueStaleEpisodePrunes(writeBatch, writeTasks, prefix, episodeCandidates, episodeMetas, minOccurredAtTicks);

        if (writeTasks.Count == 0)
        {
            return;
        }

        writeBatch.Execute();
        await Task.WhenAll(writeTasks);
    }

    private static async Task<(List<(Guid Id, ShuffleMediaKind PoolKind)> Media, List<Guid> Episodes)> ReadPoolCandidatesAsync(
        IDatabase db,
        string prefix)
    {
        var poolReadBatch = db.CreateBatch();
        var movieMembersTask = poolReadBatch.SortedSetRangeByRankAsync(ShuffleRedisKeys.PoolZSet(prefix, ShuffleMediaKind.Movie));
        var seriesMembersTask = poolReadBatch.SortedSetRangeByRankAsync(ShuffleRedisKeys.PoolZSet(prefix, ShuffleMediaKind.Series));
        var episodeMembersTask = poolReadBatch.SortedSetRangeByRankAsync(ShuffleRedisKeys.PoolZSet(prefix, ShuffleMediaKind.StandaloneEpisode));
        poolReadBatch.Execute();
        await Task.WhenAll(movieMembersTask, seriesMembersTask, episodeMembersTask);

        var mediaCandidates = new List<(Guid Id, ShuffleMediaKind PoolKind)>();
        CollectCandidates(await movieMembersTask, ShuffleMediaKind.Movie, mediaCandidates);
        CollectCandidates(await seriesMembersTask, ShuffleMediaKind.Series, mediaCandidates);

        var episodeCandidates = ParseCandidateGuids(await episodeMembersTask);
        return (mediaCandidates, episodeCandidates);
    }

    private static List<Guid> ParseCandidateGuids(RedisValue[] members)
    {
        var result = new List<Guid>(members.Length);
        for (var i = 0; i < members.Length; i++)
        {
            if (Guid.TryParse((string?)members[i], out var id))
            {
                result.Add(id);
            }
        }

        return result;
    }

    private static async Task<(RedisValue[][] MediaMetas, RedisValue[][] EpisodeMetas)> ReadCandidateMetasAsync(
        IDatabase db,
        string prefix,
        List<(Guid Id, ShuffleMediaKind PoolKind)> mediaCandidates,
        List<Guid> episodeCandidates)
    {
        var metaBatch = db.CreateBatch();
        var mediaMetaTasks = new Task<RedisValue[]>[mediaCandidates.Count];
        for (var i = 0; i < mediaCandidates.Count; i++)
        {
            mediaMetaTasks[i] = metaBatch.HashGetAsync(
                ShuffleRedisKeys.MediaHash(prefix, mediaCandidates[i].Id),
                _mediaMetaFields);
        }

        var episodeMetaTasks = new Task<RedisValue[]>[episodeCandidates.Count];
        for (var i = 0; i < episodeCandidates.Count; i++)
        {
            episodeMetaTasks[i] = metaBatch.HashGetAsync(
                ShuffleRedisKeys.EpisodeHash(prefix, episodeCandidates[i]),
                _episodeMetaFields);
        }

        metaBatch.Execute();
        var mediaMetas = mediaMetaTasks.Length > 0 ? await Task.WhenAll(mediaMetaTasks) : [];
        var episodeMetas = episodeMetaTasks.Length > 0 ? await Task.WhenAll(episodeMetaTasks) : [];
        return (mediaMetas, episodeMetas);
    }

    private static void QueueStaleMediaPrunes(
        IBatch writeBatch,
        List<Task> writeTasks,
        string prefix,
        List<(Guid Id, ShuffleMediaKind PoolKind)> mediaCandidates,
        RedisValue[][] mediaMetas,
        long minOccurredAtTicks)
    {
        for (var i = 0; i < mediaCandidates.Count; i++)
        {
            var meta = mediaMetas[i];
            if (IsFreshEntry(meta[0], minOccurredAtTicks))
            {
                continue;
            }

            var (mediaId, poolKind) = mediaCandidates[i];
            var mediaIdStr = mediaId.ToString();
            writeTasks.Add(writeBatch.KeyDeleteAsync(ShuffleRedisKeys.MediaHash(prefix, mediaId)));
            writeTasks.Add(writeBatch.KeyDeleteAsync(ShuffleRedisKeys.SeriesEpisodesSet(prefix, mediaId)));
            writeTasks.Add(writeBatch.SortedSetRemoveAsync(ShuffleRedisKeys.PoolZSet(prefix, poolKind), mediaIdStr));
            writeTasks.Add(writeBatch.SortedSetRemoveAsync(ShuffleRedisKeys.SafetyNetZSet(prefix, poolKind), mediaIdStr));

            var kind = meta[1].HasValue &&
                       int.TryParse((string?)meta[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedKind)
                ? (ShuffleMediaKind)parsedKind
                : poolKind;

            var removeCtx = new RedisIndexWriteContext(writeBatch, writeTasks, prefix, mediaIdStr);
            QueueAllIndexRemovals(removeCtx, kind, meta[2], ShuffleRedisKeys.PlatformIndexSet);
            QueueAllIndexRemovals(removeCtx, kind, meta[3], ShuffleRedisKeys.MoodIndexSet);
            QueueAllIndexRemovals(removeCtx, kind, meta[4], ShuffleRedisKeys.GenreIndexSet);
        }
    }

    private static void QueueStaleEpisodePrunes(
        IBatch writeBatch,
        List<Task> writeTasks,
        string prefix,
        List<Guid> episodeCandidates,
        RedisValue[][] episodeMetas,
        long minOccurredAtTicks)
    {
        for (var i = 0; i < episodeCandidates.Count; i++)
        {
            var meta = episodeMetas[i];
            if (!IsFreshEntry(meta[0], minOccurredAtTicks))
            {
                QueueEpisodeDeletion(writeBatch, writeTasks, prefix, episodeCandidates[i], meta);
            }
        }
    }

    public async Task RemoveTaxonomyIndexAsync(
        string taxonomyType,
        Guid taxonomyId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var db = redis.GetDatabase();
        var prefix = Prefix;

        Func<string, Guid, ShuffleMediaKind, string>? keyBuilder = taxonomyType.ToLowerInvariant() switch
        {
            "platform" => ShuffleRedisKeys.PlatformIndexSet,
            "mood" => ShuffleRedisKeys.MoodIndexSet,
            "genre" => ShuffleRedisKeys.GenreIndexSet,
            _ => null
        };

        if (keyBuilder is null)
        {
            return;
        }

        RedisKey[] keys =
        [
            keyBuilder(prefix, taxonomyId, ShuffleMediaKind.Movie),
            keyBuilder(prefix, taxonomyId, ShuffleMediaKind.Series),
            keyBuilder(prefix, taxonomyId, ShuffleMediaKind.StandaloneEpisode)
        ];

        await db.KeyDeleteAsync(keys);
    }

    public async Task<ShuffleMediaAffinityContext?> GetMediaAffinityContextAsync(
        Guid mediaId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var db = redis.GetDatabase();
        var values = await db.HashGetAsync(
            ShuffleRedisKeys.MediaHash(Prefix, mediaId),
            [FieldMoodIds, FieldGenreIds, "director"]);

        if (!values[0].HasValue && !values[1].HasValue && !values[2].HasValue)
        {
            return null;
        }

        var moodIds = ParseGuids(values[0]);
        var genreIds = ParseGuids(values[1]);
        var director = values[2].HasValue ? (string?)values[2] : null;
        if (string.IsNullOrWhiteSpace(director))
        {
            director = null;
        }

        return new ShuffleMediaAffinityContext(mediaId, moodIds, genreIds, director);
    }

    public async Task<ShuffleMediaSnapshot?> GetMediaByIdAsync(
        Guid mediaId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var db = redis.GetDatabase();
        var payload = await db.HashGetAsync(ShuffleRedisKeys.MediaHash(Prefix, mediaId), FieldPayload);
        if (!payload.HasValue)
        {
            return null;
        }

        return JsonSerializer.Deserialize<ShuffleMediaSnapshot>((byte[])payload!, _jsonOptions);
    }

    public async Task<ShuffleEpisodeSnapshot?> GetEpisodeByIdAsync(
        Guid episodeId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var db = redis.GetDatabase();
        var payload = await db.HashGetAsync(ShuffleRedisKeys.EpisodeHash(Prefix, episodeId), FieldPayload);
        if (!payload.HasValue)
        {
            return null;
        }

        return JsonSerializer.Deserialize<ShuffleEpisodeSnapshot>((byte[])payload!, _jsonOptions);
    }

    public async Task<bool> IsPoolWarmedAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var db = redis.GetDatabase();
        return await db.KeyExistsAsync(ShuffleRedisKeys.WarmMarker(Prefix));
    }

    public async Task MarkPoolWarmedAsync(ShufflePoolWarmState state, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var db = redis.GetDatabase();
        var bytes = JsonSerializer.SerializeToUtf8Bytes(state, _jsonOptions);
        var ttl = ShuffleRedisKeys.WithJitter(ShuffleRedisKeys.BaseWarmMarkerTtl);
        await db.StringSetAsync(ShuffleRedisKeys.WarmMarker(Prefix), bytes, ttl);
    }

    public async Task ClearWarmMarkerAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var db = redis.GetDatabase();
        await db.KeyDeleteAsync(ShuffleRedisKeys.WarmMarker(Prefix));
    }

    public async Task<ShufflePoolStatistics> GetPoolStatisticsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var db = redis.GetDatabase();
        var prefix = Prefix;

        var batch = db.CreateBatch();
        var markerTask = batch.StringGetAsync(ShuffleRedisKeys.WarmMarker(prefix));
        var moviesTask = batch.SortedSetLengthAsync(ShuffleRedisKeys.PoolZSet(prefix, ShuffleMediaKind.Movie));
        var seriesTask = batch.SortedSetLengthAsync(ShuffleRedisKeys.PoolZSet(prefix, ShuffleMediaKind.Series));
        var episodesTask = batch.SortedSetLengthAsync(ShuffleRedisKeys.PoolZSet(prefix, ShuffleMediaKind.StandaloneEpisode));
        var snMoviesTask = batch.SortedSetLengthAsync(ShuffleRedisKeys.SafetyNetZSet(prefix, ShuffleMediaKind.Movie));
        var snSeriesTask = batch.SortedSetLengthAsync(ShuffleRedisKeys.SafetyNetZSet(prefix, ShuffleMediaKind.Series));
        var snEpisodesTask = batch.SortedSetLengthAsync(ShuffleRedisKeys.SafetyNetZSet(prefix, ShuffleMediaKind.StandaloneEpisode));

        batch.Execute();
        await Task.WhenAll(markerTask, moviesTask, seriesTask, episodesTask, snMoviesTask, snSeriesTask, snEpisodesTask);

        var markerValue = await markerTask;
        DateTime? lastWarmedAtUtc = null;
        if (markerValue.HasValue)
        {
            var warmState = JsonSerializer.Deserialize<ShufflePoolWarmState>((byte[])markerValue!, _jsonOptions);
            lastWarmedAtUtc = warmState?.WarmedAtUtc;
        }

        return new ShufflePoolStatistics(
            IsWarmed: markerValue.HasValue,
            MoviesCount: (int)await moviesTask,
            SeriesCount: (int)await seriesTask,
            StandaloneEpisodesCount: (int)await episodesTask,
            SafetyNetMoviesCount: (int)await snMoviesTask,
            SafetyNetSeriesCount: (int)await snSeriesTask,
            SafetyNetEpisodesCount: (int)await snEpisodesTask,
            LastWarmedAtUtc: lastWarmedAtUtc);
    }

    private static void QueueEpisodeDeletion(
        IBatch writeBatch,
        List<Task> writeTasks,
        string prefix,
        Guid episodeId,
        RedisValue[] existing)
    {
        var epIdStr = episodeId.ToString();
        writeTasks.Add(writeBatch.KeyDeleteAsync(ShuffleRedisKeys.EpisodeHash(prefix, episodeId)));
        writeTasks.Add(writeBatch.SortedSetRemoveAsync(ShuffleRedisKeys.PoolZSet(prefix, ShuffleMediaKind.StandaloneEpisode), epIdStr));
        writeTasks.Add(writeBatch.SortedSetRemoveAsync(ShuffleRedisKeys.SafetyNetZSet(prefix, ShuffleMediaKind.StandaloneEpisode), epIdStr));

        if (existing[1].HasValue && Guid.TryParse((string?)existing[1], out var seriesId))
        {
            writeTasks.Add(writeBatch.SetRemoveAsync(ShuffleRedisKeys.SeriesEpisodesSet(prefix, seriesId), epIdStr));
        }

        var removeCtx = new RedisIndexWriteContext(writeBatch, writeTasks, prefix, epIdStr);
        QueueAllIndexRemovals(removeCtx, ShuffleMediaKind.StandaloneEpisode, existing[2], ShuffleRedisKeys.PlatformIndexSet);
        QueueAllIndexRemovals(removeCtx, ShuffleMediaKind.StandaloneEpisode, existing[3], ShuffleRedisKeys.MoodIndexSet);
        QueueAllIndexRemovals(removeCtx, ShuffleMediaKind.StandaloneEpisode, existing[4], ShuffleRedisKeys.GenreIndexSet);
    }

    private static void QueueIndexAdditions(
        RedisIndexWriteContext ctx,
        ShuffleMediaKind kind,
        IReadOnlyList<Guid> ids,
        TimeSpan ttl,
        Func<string, Guid, ShuffleMediaKind, string> keyFactory)
    {
        for (var i = 0; i < ids.Count; i++)
        {
            var setKey = keyFactory(ctx.Prefix, ids[i], kind);
            ctx.Tasks.Add(ctx.Batch.SetAddAsync(setKey, ctx.MemberId));
            ctx.Tasks.Add(ctx.Batch.KeyExpireAsync(setKey, ttl));
        }
    }

    private static void QueueStaleIndexRemovals(
        RedisIndexWriteContext ctx,
        ShuffleMediaKind oldKind,
        ShuffleMediaKind newKind,
        RedisValue oldCsv,
        IReadOnlyList<Guid> newIds,
        Func<string, Guid, ShuffleMediaKind, string> keyFactory)
    {
        if (!oldCsv.HasValue)
        {
            return;
        }

        var oldIds = ParseGuids(oldCsv);
        if (oldIds.Count == 0)
        {
            return;
        }

        var newSet = newIds.Count > 0 ? new HashSet<Guid>(newIds) : null;
        for (var i = 0; i < oldIds.Count; i++)
        {
            var oldId = oldIds[i];
            if (oldKind != newKind || newSet is null || !newSet.Contains(oldId))
            {
                ctx.Tasks.Add(ctx.Batch.SetRemoveAsync(keyFactory(ctx.Prefix, oldId, oldKind), ctx.MemberId));
            }
        }
    }

    private static void QueueAllIndexRemovals(
        RedisIndexWriteContext ctx,
        ShuffleMediaKind kind,
        RedisValue oldCsv,
        Func<string, Guid, ShuffleMediaKind, string> keyFactory)
    {
        var oldIds = ParseGuids(oldCsv);
        for (var i = 0; i < oldIds.Count; i++)
        {
            ctx.Tasks.Add(ctx.Batch.SetRemoveAsync(keyFactory(ctx.Prefix, oldIds[i], kind), ctx.MemberId));
        }
    }

    private static void CollectCandidates(
        RedisValue[] members,
        ShuffleMediaKind kind,
        List<(Guid Id, ShuffleMediaKind PoolKind)> destination)
    {
        for (var i = 0; i < members.Length; i++)
        {
            if (Guid.TryParse((string?)members[i], out var id))
            {
                destination.Add((id, kind));
            }
        }
    }

    private static bool IsFreshEntry(RedisValue ticksValue, long minOccurredAtTicks) =>
        ticksValue.HasValue &&
        long.TryParse((string?)ticksValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var entryTicks) &&
        entryTicks >= minOccurredAtTicks;

    private static string JoinGuids(IReadOnlyList<Guid> ids) =>
        ids.Count == 0 ? string.Empty : string.Join(',', ids);

    private static List<Guid> ParseGuids(RedisValue csv)
    {
        if (!csv.HasValue)
        {
            return [];
        }

        var raw = (string?)csv;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        var parts = raw.Split(_commaSeparator, StringSplitOptions.RemoveEmptyEntries);
        var result = new List<Guid>(parts.Length);
        for (var i = 0; i < parts.Length; i++)
        {
            if (Guid.TryParse(parts[i], out var guid))
            {
                result.Add(guid);
            }
        }

        return result;
    }
}
