using Microsoft.EntityFrameworkCore;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Infrastructure.Extensions;

namespace ShuffleSeries.Catalog.Infrastructure.Persistence.Repositories;

internal sealed class SeriesRepository : ISeriesRepository
{
    private readonly CatalogDbContext _context;

    public SeriesRepository(CatalogDbContext context)
    {
        _context = context;
    }

    public void Add(Series series) => _context.Series.Add(series);
    public void Update(Series series) => _context.Series.Update(series);
    public void Delete(Series series) => _context.Series.Remove(series);

    public async Task<Series?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Series.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Series?> GetByIdWithTranslationsAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Series
            .AsNoTracking()
            .Include(x => x.Translations)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Series?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Series
            .Include(x => x.Translations)
            .Include(x => x.MediaPlatforms).ThenInclude(p => p.Platform)
            .Include(x => x.MediaMoods).ThenInclude(m => m.Mood)
            .Include(x => x.MediaGenres).ThenInclude(g => g.Genre)
            .Include(x => x.Seasons.OrderBy(s => s.SeasonNumber))
                .ThenInclude(s => s.Episodes.OrderBy(e => e.EpisodeNumber))
                .ThenInclude(e => e.Translations)
            .Include(x => x.Seasons)
                .ThenInclude(s => s.Episodes)
                .ThenInclude(e => e.EpisodePlatforms).ThenInclude(ep => ep.Platform)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<bool> ExistsByTitleAsync(string title, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title))
            return false;

        var normalized = title.Trim().ToLowerInvariant();

        return await _context.Series
            .AnyAsync(s => s.Translations.Any(t => t.Title.ToLower() == normalized), cancellationToken);
    }

    public async Task<bool> ExistsByImdbIdAsync(string imdbId, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imdbId))
            return false;

        var normalized = imdbId.Trim();

        return await _context.Media
            .AnyAsync(m => m.ImdbId == normalized && (!excludeId.HasValue || m.Id != excludeId.Value), cancellationToken);
    }

    public async Task<(IReadOnlyList<Series> Items, int TotalCount)> GetPagedListAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var totalCount = await _context.Series.CountAsync(cancellationToken);
        if (totalCount == 0)
        {
            return ([], 0);
        }

        var items = await _context.Series
            .AsNoTracking()
            .Include(x => x.Translations)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ApplyPagination(page, pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
