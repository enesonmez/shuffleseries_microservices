namespace ShuffleSeries.ShuffleEngine.Application.Common.Models;

public sealed record ShufflePoolWarmState(
    int MoviesCount,
    int SeriesCount,
    int StandaloneEpisodesCount,
    int SafetyNetTotalCount,
    DateTime WarmedAtUtc);
