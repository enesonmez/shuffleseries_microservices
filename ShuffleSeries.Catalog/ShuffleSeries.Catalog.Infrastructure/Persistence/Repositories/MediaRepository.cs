using Microsoft.EntityFrameworkCore;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Enums;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Infrastructure.Extensions;

namespace ShuffleSeries.Catalog.Infrastructure.Persistence.Repositories;

internal sealed class MediaRepository : MediaRepositoryBase<Media>, IMediaRepository
{
    public MediaRepository(CatalogDbContext context) : base(context)
    {
    }

    public async Task<Media?> GetByIdWithPlatformsAsync(Guid id, CancellationToken cancellationToken = default) =>
        await Context.Media
            .Include(x => x.MediaPlatforms)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Media?> GetByIdWithMoodsAsync(Guid id, CancellationToken cancellationToken = default) =>
        await Context.Media
            .Include(x => x.MediaMoods)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Media?> GetByIdWithGenresAsync(Guid id, CancellationToken cancellationToken = default) =>
        await Context.Media
            .Include(x => x.MediaGenres)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Media?> GetByIdWithTranslationsAsync(Guid id, CancellationToken cancellationToken = default) =>
        await Context.Media
            .Include(x => x.Translations)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Media> Items, int TotalCount)> GetAdminMediaPagedListAsync(
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
        CancellationToken cancellationToken = default)
    {
        var query = Context.Media.AsNoTracking();

        if (isDeleted)
        {
            query = query.IgnoreQueryFilters().Where(x => x.IsDeleted);
        }

        if (mediaType.HasValue)
        {
            query = query.Where(x => x.MediaType == mediaType.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchPattern = $"%{search.Trim()}%";
            query = query.Where(x =>
                x.Translations.Any(t => EF.Functions.ILike(t.Title, searchPattern)) ||
                (x.ImdbId != null && EF.Functions.ILike(x.ImdbId, searchPattern)));
        }

        if (platformId.HasValue)
        {
            query = query.Where(x => x.MediaPlatforms.Any(p => p.PlatformId == platformId.Value));
        }

        if (moodId.HasValue)
        {
            query = query.Where(x => x.MediaMoods.Any(m => m.MoodId == moodId.Value));
        }

        if (genreId.HasValue)
        {
            query = query.Where(x => x.MediaGenres.Any(g => g.GenreId == genreId.Value));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        if (totalCount == 0)
        {
            return (Array.Empty<Media>(), 0);
        }

        var normalizedSortBy = sortBy?.Trim().ToLowerInvariant();
        query = (normalizedSortBy, sortDescending) switch
        {
            ("imdbrating", true) => query.OrderByDescending(x => x.ImdbRating).ThenByDescending(x => x.CreatedAtUtc),
            ("imdbrating", false) => query.OrderBy(x => x.ImdbRating).ThenByDescending(x => x.CreatedAtUtc),
            ("releaseyear", true) => query.OrderByDescending(x => x.ReleaseYear).ThenByDescending(x => x.CreatedAtUtc),
            ("releaseyear", false) => query.OrderBy(x => x.ReleaseYear).ThenByDescending(x => x.CreatedAtUtc),
            ("title", true) => query.OrderByDescending(x => x.Translations.OrderBy(t => t.LanguageCode).Select(t => t.Title).FirstOrDefault()).ThenByDescending(x => x.CreatedAtUtc),
            ("title", false) => query.OrderBy(x => x.Translations.OrderBy(t => t.LanguageCode).Select(t => t.Title).FirstOrDefault()).ThenByDescending(x => x.CreatedAtUtc),
            ("createdat", false) => query.OrderBy(x => x.CreatedAtUtc),
            _ => query.OrderByDescending(x => x.CreatedAtUtc)
        };

        var items = await query
            .Include(x => x.Translations)
            .Include(x => x.MediaPlatforms).ThenInclude(p => p.Platform)
            .Include(x => x.MediaMoods).ThenInclude(m => m.Mood)
            .Include(x => x.MediaGenres).ThenInclude(g => g.Genre)
            .ApplyPagination(page, pageSize)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
