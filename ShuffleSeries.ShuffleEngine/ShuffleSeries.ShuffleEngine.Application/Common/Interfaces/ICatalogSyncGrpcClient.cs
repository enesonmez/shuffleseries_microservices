using ShuffleSeries.ShuffleEngine.Application.Common.Models;
using ShuffleSeries.ShuffleEngine.Domain.Models;

namespace ShuffleSeries.ShuffleEngine.Application.Common.Interfaces;

public interface ICatalogSyncGrpcClient
{
    Task<ShuffleMediaSnapshot?> GetMediaByIdAsync(Guid mediaId, CancellationToken cancellationToken = default);

    Task<ShuffleEpisodeSnapshot?> GetStandaloneEpisodeByIdAsync(
        Guid episodeId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ShuffleEpisodeSnapshot>> GetStandaloneEpisodesBySeriesIdAsync(
        Guid seriesId,
        CancellationToken cancellationToken = default);

    Task<CatalogPagedResult<ShuffleMediaSnapshot>> GetBulkMediaPageAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<CatalogPagedResult<ShuffleEpisodeSnapshot>> GetStandaloneEpisodesPageAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}
