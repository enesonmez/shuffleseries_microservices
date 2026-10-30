using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Domain.Repositories;

public interface IEpisodeRepository : IRepository
{
    Task<Episode?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Episode?> GetByIdForDetailAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Episode?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Episode>> GetStandaloneEpisodesAsync(int? limit = null, CancellationToken cancellationToken = default);
    void Update(Episode episode);
    void Delete(Episode episode);
}
