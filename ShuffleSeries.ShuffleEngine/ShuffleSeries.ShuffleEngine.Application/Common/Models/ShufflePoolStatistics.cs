namespace ShuffleSeries.ShuffleEngine.Application.Common.Models;

public sealed record ShufflePoolStatistics(
    bool IsWarmed,
    int MoviesCount,
    int SeriesCount,
    int StandaloneEpisodesCount,
    int SafetyNetMoviesCount,
    int SafetyNetSeriesCount,
    int SafetyNetEpisodesCount,
    DateTime? LastWarmedAtUtc);
