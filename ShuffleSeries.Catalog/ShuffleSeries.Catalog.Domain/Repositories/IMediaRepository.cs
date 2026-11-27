using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Enums;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Domain.Repositories;

public interface IMediaRepository : IRepository<Media>
{
    Task<Media?> GetByIdWithPlatformsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Media?> GetByIdWithMoodsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Media?> GetByIdWithGenresAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Media?> GetByIdWithTranslationsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Media?> GetByIdForShuffleAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Media> Items, int TotalCount)> GetBulkForShuffleAsync(
        MediaType? mediaType,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Media> Items, int TotalCount)> GetAdminMediaPagedListAsync(
        AdminMediaFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
