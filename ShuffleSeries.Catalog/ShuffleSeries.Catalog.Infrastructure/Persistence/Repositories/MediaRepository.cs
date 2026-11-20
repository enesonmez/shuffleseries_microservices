using Microsoft.EntityFrameworkCore;
using ShuffleSeries.Catalog.Domain.Entities;
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
        AdminMediaFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyAdminMediaFilters(Context.Media.AsNoTracking(), filter);

        var totalCount = await query.CountAsync(cancellationToken);
        if (totalCount == 0)
        {
            return (Array.Empty<Media>(), 0);
        }

        query = ApplyAdminMediaSorting(query, filter);

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

    private static IQueryable<Media> ApplyAdminMediaFilters(IQueryable<Media> query, AdminMediaFilter filter)
    {
        if (filter.IsDeleted)
        {
            query = query.IgnoreQueryFilters().Where(x => x.IsDeleted);
        }

        if (filter.MediaType.HasValue)
        {
            query = query.Where(x => x.MediaType == filter.MediaType.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var escaped = filter.Search.Trim()
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("%", "\\%", StringComparison.Ordinal)
                .Replace("_", "\\_", StringComparison.Ordinal);
            var searchPattern = $"%{escaped}%";
            query = query.Where(x =>
                x.Translations.Any(t => EF.Functions.ILike(t.Title, searchPattern, "\\")) ||
                (x.ImdbId != null && EF.Functions.ILike(x.ImdbId, searchPattern, "\\")));
        }

        if (filter.PlatformId.HasValue)
        {
            query = query.Where(x => x.MediaPlatforms.Any(p => p.PlatformId == filter.PlatformId.Value));
        }

        if (filter.MoodId.HasValue)
        {
            query = query.Where(x => x.MediaMoods.Any(m => m.MoodId == filter.MoodId.Value));
        }

        if (filter.GenreId.HasValue)
        {
            query = query.Where(x => x.MediaGenres.Any(g => g.GenreId == filter.GenreId.Value));
        }

        return query;
    }

    private static IQueryable<Media> ApplyAdminMediaSorting(IQueryable<Media> query, AdminMediaFilter filter)
    {
        var lang = filter.TargetLanguage?.Trim().ToLowerInvariant() ?? "tr";
        var normalizedSortBy = filter.SortBy?.Trim().ToLowerInvariant();

        return (normalizedSortBy, filter.SortDescending) switch
        {
            ("imdbrating", true) => query.OrderByDescending(x => x.ImdbRating).ThenByDescending(x => x.CreatedAtUtc),
            ("imdbrating", false) => query.OrderBy(x => x.ImdbRating).ThenByDescending(x => x.CreatedAtUtc),
            ("releaseyear", true) => query.OrderByDescending(x => x.ReleaseYear).ThenByDescending(x => x.CreatedAtUtc),
            ("releaseyear", false) => query.OrderBy(x => x.ReleaseYear).ThenByDescending(x => x.CreatedAtUtc),
            ("title", true) => query.OrderByDescending(x => x.Translations.Where(t => t.LanguageCode == lang).Select(t => t.Title).FirstOrDefault() ?? x.Translations.OrderBy(t => t.LanguageCode).Select(t => t.Title).FirstOrDefault()).ThenByDescending(x => x.CreatedAtUtc),
            ("title", false) => query.OrderBy(x => x.Translations.Where(t => t.LanguageCode == lang).Select(t => t.Title).FirstOrDefault() ?? x.Translations.OrderBy(t => t.LanguageCode).Select(t => t.Title).FirstOrDefault()).ThenByDescending(x => x.CreatedAtUtc),
            ("createdat", false) => query.OrderBy(x => x.CreatedAtUtc),
            _ => query.OrderByDescending(x => x.CreatedAtUtc)
        };
    }
}
