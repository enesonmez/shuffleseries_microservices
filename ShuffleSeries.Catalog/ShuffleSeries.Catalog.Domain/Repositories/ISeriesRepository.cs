using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Domain.Repositories;

public interface ISeriesRepository : IRepository<Series>
{
    Task<bool> ExistsByTitleAsync(string title, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Series> Items, int TotalCount)> GetPagedListAsync(int page, int pageSize,
        CancellationToken cancellationToken = default);
}