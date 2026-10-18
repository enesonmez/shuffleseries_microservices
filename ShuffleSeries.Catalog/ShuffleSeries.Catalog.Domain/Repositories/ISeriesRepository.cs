using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Domain.Repositories;

public interface ISeriesRepository : IRepository<Series>
{
    Task<Series?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Series?> GetByIdWithTranslationsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByTitleAsync(string title, CancellationToken cancellationToken = default);
    Task<bool> ExistsByImdbIdAsync(string imdbId, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Series> Items, int TotalCount)> GetPagedListAsync(int page, int pageSize, CancellationToken cancellationToken = default);
}
