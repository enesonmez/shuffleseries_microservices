namespace ShuffleSeries.ShuffleEngine.Application.Features.CacheSync.Responses;

public sealed record WarmShuffleCacheResponse(
    bool WasSkipped,
    int MoviesSynced,
    int SeriesSynced,
    int StandaloneEpisodesSynced,
    int SafetyNetItemsCount,
    long DurationMs,
    DateTime WarmedAtUtc);
