using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Domain.Repositories;

public interface ISeriesRepository : IRepository<Series>
{
    Task<Series?> GetByIdForDetailAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Series?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Series?> GetByIdWithSeasonsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByTmdbIdAsync(int tmdbId, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsByImdbIdAsync(string imdbId, Guid? excludeId = null, CancellationToken cancellationToken = default);
}
