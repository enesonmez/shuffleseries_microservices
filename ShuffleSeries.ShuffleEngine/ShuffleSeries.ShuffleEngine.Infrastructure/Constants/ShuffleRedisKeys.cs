using ShuffleSeries.ShuffleEngine.Domain.Constants;
using ShuffleSeries.ShuffleEngine.Domain.Enums;

namespace ShuffleSeries.ShuffleEngine.Infrastructure.Constants;

internal static class ShuffleRedisKeys
{
    public static readonly TimeSpan BasePoolTtl = TimeSpan.FromHours(24);
    public static readonly TimeSpan BaseWarmMarkerTtl = TimeSpan.FromHours(12);
    public static readonly TimeSpan BaseCooldownTtl = TimeSpan.FromHours(ShuffleDomainConstants.CooldownHours);
    public static readonly TimeSpan BaseAffinityTtl = TimeSpan.FromDays(ShuffleDomainConstants.AffinityRetentionDays);
    public static readonly TimeSpan WarmLockLease = TimeSpan.FromSeconds(120);

    public static string MediaHash(string prefix, Guid mediaId) =>
        $"{prefix}shuffle:media:{mediaId}";

    public static string EpisodeHash(string prefix, Guid episodeId) =>
        $"{prefix}shuffle:episode:{episodeId}";

    public static string PoolZSet(string prefix, ShuffleMediaKind kind) =>
        $"{prefix}shuffle:pool:{KindSegment(kind)}";

    public static string SafetyNetZSet(string prefix, ShuffleMediaKind kind) =>
        $"{prefix}shuffle:safety-net:{KindSegment(kind)}";

    public static string PlatformIndexSet(string prefix, Guid platformId, ShuffleMediaKind kind) =>
        $"{prefix}shuffle:idx:platform:{platformId}:{KindSegment(kind)}";

    public static string MoodIndexSet(string prefix, Guid moodId, ShuffleMediaKind kind) =>
        $"{prefix}shuffle:idx:mood:{moodId}:{KindSegment(kind)}";

    public static string GenreIndexSet(string prefix, Guid genreId, ShuffleMediaKind kind) =>
        $"{prefix}shuffle:idx:genre:{genreId}:{KindSegment(kind)}";

    public static string SeriesEpisodesSet(string prefix, Guid seriesId) =>
        $"{prefix}shuffle:idx:series:{seriesId}:episodes";

    public static string WarmMarker(string prefix) =>
        $"{prefix}shuffle:meta:warmed";

    public static string WarmDistributedLock(string prefix) =>
        $"{prefix}shuffle:lock:warming";

    public static string UserCooldownZSet(string prefix, Guid userId) =>
        $"{prefix}cooldown:user:{userId}";

    public static string UserAffinityHash(string prefix, Guid userId) =>
        $"{prefix}shuffle:user:{userId}:affinity";

    public static string UserEntitlementHash(string prefix, Guid userId) =>
        $"{prefix}shuffle:user:{userId}:entitlement";

    public static string KindSegment(ShuffleMediaKind kind) => kind switch
    {
        ShuffleMediaKind.Movie => "movies",
        ShuffleMediaKind.Series => "series",
        ShuffleMediaKind.StandaloneEpisode => "episodes",
        _ => "movies"
    };

    public static TimeSpan WithJitter(TimeSpan baseTtl, int maxJitterSeconds = 300) =>
        baseTtl.Add(TimeSpan.FromSeconds(Random.Shared.Next(0, maxJitterSeconds + 1)));
}
