using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Domain.Repositories;

public interface ISeriesRepository : IRepository<Series>
{
    Task<bool> ExistsByTitleAsync(string title, CancellationToken cancellationToken = default);
}