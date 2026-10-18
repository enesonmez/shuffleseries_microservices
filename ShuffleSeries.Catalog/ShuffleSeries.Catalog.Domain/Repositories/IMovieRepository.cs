using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Domain.Repositories;

public interface IMovieRepository : IRepository<Movie>
{
    Task<Movie?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
}
