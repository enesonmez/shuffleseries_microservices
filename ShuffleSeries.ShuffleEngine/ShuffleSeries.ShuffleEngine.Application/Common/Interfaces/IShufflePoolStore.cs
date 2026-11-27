using ShuffleSeries.ShuffleEngine.Application.Common.Models;
using ShuffleSeries.ShuffleEngine.Domain.Models;

namespace ShuffleSeries.ShuffleEngine.Application.Common.Interfaces;

public interface IShufflePoolStore
{
    Task UpsertMediaBatchAsync(
        IReadOnlyList<ShuffleMediaSnapshot> items,
        long occurredAtTicks,
        CancellationToken cancellationToken = default);

    Task RemoveMediaAsync(Guid mediaId, CancellationToken cancellationToken = default);

    Task UpsertEpisodesBatchAsync(
        IReadOnlyList<ShuffleEpisodeSnapshot> items,
        long occurredAtTicks,
        CancellationToken cancellationToken = default);

    Task RemoveEpisodeAsync(Guid episodeId, CancellationToken cancellationToken = default);

    Task ReconcileSeriesEpisodesAsync(
        Guid seriesId,
        IReadOnlyList<ShuffleEpisodeSnapshot> activeEpisodes,
        long occurredAtTicks,
        CancellationToken cancellationToken = default);

    Task PruneStaleEntriesAsync(
        long minOccurredAtTicks,
        CancellationToken cancellationToken = default);

    Task RemoveTaxonomyIndexAsync(
        string taxonomyType,
        Guid taxonomyId,
        CancellationToken cancellationToken = default);

    Task<ShuffleMediaAffinityContext?> GetMediaAffinityContextAsync(
        Guid mediaId,
        CancellationToken cancellationToken = default);

    Task<ShuffleMediaSnapshot?> GetMediaByIdAsync(
        Guid mediaId,
        CancellationToken cancellationToken = default);

    Task<ShuffleEpisodeSnapshot?> GetEpisodeByIdAsync(
        Guid episodeId,
        CancellationToken cancellationToken = default);

    Task<bool> IsPoolWarmedAsync(CancellationToken cancellationToken = default);

    Task MarkPoolWarmedAsync(ShufflePoolWarmState state, CancellationToken cancellationToken = default);

    Task ClearWarmMarkerAsync(CancellationToken cancellationToken = default);

    Task<ShufflePoolStatistics> GetPoolStatisticsAsync(CancellationToken cancellationToken = default);
}
