using ShuffleSeries.ShuffleEngine.Application.Common.Interfaces;
using ShuffleSeries.ShuffleEngine.Application.Features.CacheSync.Responses;

namespace ShuffleSeries.ShuffleEngine.Application.Features.CacheSync.Queries.GetShufflePoolStatus;

public sealed class GetShufflePoolStatusQueryHandler(IShufflePoolStore poolStore)
{
    public async Task<ShufflePoolStatusResponse> Handle(
        GetShufflePoolStatusQuery _,
        CancellationToken cancellationToken)
    {
        var stats = await poolStore.GetPoolStatisticsAsync(cancellationToken);

        return new ShufflePoolStatusResponse(
            stats.IsWarmed,
            stats.MoviesCount,
            stats.SeriesCount,
            stats.StandaloneEpisodesCount,
            stats.SafetyNetMoviesCount,
            stats.SafetyNetSeriesCount,
            stats.SafetyNetEpisodesCount,
            stats.LastWarmedAtUtc);
    }
}
