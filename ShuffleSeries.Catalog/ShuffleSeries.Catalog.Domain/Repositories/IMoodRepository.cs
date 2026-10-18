using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Domain.Repositories;

public interface IMoodRepository : IRepository<Mood>
{
    Task<Mood?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<Mood?> GetByIdWithTranslationsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Mood>> GetActiveMoodsWithTranslationsAsync(CancellationToken cancellationToken = default);
}
