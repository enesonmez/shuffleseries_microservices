using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Domain.Repositories;

public interface IPlatformRepository : IRepository<Platform>
{
    Task<Platform?> GetByIdForDetailAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Platform>> GetActivePlatformsAsync(CancellationToken cancellationToken = default);
}
