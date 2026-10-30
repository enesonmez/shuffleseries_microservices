using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Domain.Repositories;

public interface IMovieRepository : IRepository<Movie>
{
    Task<Movie?> GetByIdForDetailAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Movie?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByTmdbIdAsync(int tmdbId, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsByImdbIdAsync(string imdbId, Guid? excludeId = null, CancellationToken cancellationToken = default);
}
