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
    Task<(IReadOnlyList<Media> Items, int TotalCount)> GetAdminMediaPagedListAsync(
        MediaType? mediaType,
        string? search,
        Guid? platformId,
        Guid? moodId,
        Guid? genreId,
        bool isDeleted,
        string? sortBy,
        bool sortDescending,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
