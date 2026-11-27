namespace ShuffleSeries.ShuffleEngine.Application.Features.CacheSync.Responses;

public sealed record ShufflePoolStatusResponse(
    bool IsWarmed,
    int MoviesCount,
    int SeriesCount,
    int StandaloneEpisodesCount,
    int SafetyNetMoviesCount,
    int SafetyNetSeriesCount,
    int SafetyNetEpisodesCount,
    DateTime? LastWarmedAtUtc);
