using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Domain.Repositories;

public interface IGenreRepository : IRepository<Genre>
{
    Task<Genre?> GetByIdForDetailAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Genre?> GetByIdWithTranslationsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Genre>> GetActiveGenresWithTranslationsAsync(CancellationToken cancellationToken = default);
}
