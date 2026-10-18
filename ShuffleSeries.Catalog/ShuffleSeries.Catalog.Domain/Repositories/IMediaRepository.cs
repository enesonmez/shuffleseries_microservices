using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Domain.Repositories;

public interface IMediaRepository : IRepository<Media>
{
    Task<Media?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByImdbIdAsync(string imdbId, CancellationToken cancellationToken = default);
}
