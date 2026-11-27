using Microsoft.EntityFrameworkCore;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Shared.Core.Infrastructure.Extensions;

namespace ShuffleSeries.Catalog.Infrastructure.Persistence.Repositories;

internal sealed class EpisodeRepository : IEpisodeRepository
{
    private const int DefaultStandaloneLimit = 50;
    private const int MaxStandaloneLimit = 100;

    private readonly CatalogDbContext _context;

    public EpisodeRepository(CatalogDbContext context)
    {
        _context = context;
    }

    public void Update(Episode episode)
    {
        if (_context.Entry(episode).State == EntityState.Detached)
        {
            _context.Episodes.Update(episode);
        }
    }

    public void Delete(Episode episode) => _context.Episodes.Remove(episode);

    public async Task<Episode?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Episodes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Episode?> GetByIdForDetailAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Episodes
            .AsNoTracking()
            .Include(x => x.Translations)
            .Include(x => x.EpisodePlatforms).ThenInclude(p => p.Platform)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Episode?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Episodes
            .Include(x => x.Translations)
            .Include(x => x.EpisodePlatforms)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<int?> GetConflictingSeasonNumberAsync(
        Guid seasonId,
        int episodeNumber,
        Guid excludeEpisodeId,
        CancellationToken cancellationToken = default)
    {
        var exists = await _context.Episodes
            .AsNoTracking()
            .AnyAsync(e => e.SeasonId == seasonId && e.EpisodeNumber == episodeNumber && e.Id != excludeEpisodeId, cancellationToken);

        if (!exists)
        {
            return null;
        }

        var seasonNumber = await _context.Seasons
            .AsNoTracking()
            .Where(s => s.Id == seasonId)
            .Select(s => (int?)s.SeasonNumber)
            .FirstOrDefaultAsync(cancellationToken);

        return seasonNumber ?? 1;
    }

    public async Task<IReadOnlyList<Episode>> GetStandaloneEpisodesAsync(int? limit = null, CancellationToken cancellationToken = default)
    {
        var effectiveLimit = NormalizeStandaloneLimit(limit);

        return await _context.Episodes
            .AsNoTracking()
            .Where(x => x.IsStandalone)
            .Include(x => x.Translations)
            .Include(x => x.EpisodePlatforms).ThenInclude(p => p.Platform)
            .AsSplitQuery()
            .OrderByDescending(x => x.ImdbRating)
            .Take(effectiveLimit)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<StandaloneEpisodeContext> Items, int TotalCount)> GetStandaloneEpisodesWithSeriesContextAsync(
        int page,
        int pageSize,
        Guid? seriesId = null,
        CancellationToken cancellationToken = default)
    {
        var baseQuery = _context.Episodes
            .AsNoTracking()
            .Where(x => x.IsStandalone &&
                        _context.Seasons.Any(s => s.Id == x.SeasonId &&
                                                  (!seriesId.HasValue || s.SeriesId == seriesId.Value) &&
                                                  _context.Series.Any(sr => sr.Id == s.SeriesId)));

        var totalCount = await baseQuery.CountAsync(cancellationToken);
        if (totalCount == 0)
        {
            return (Array.Empty<StandaloneEpisodeContext>(), 0);
        }

        var episodes = await baseQuery
            .Include(x => x.Translations)
            .Include(x => x.EpisodePlatforms.Where(ep => ep.Platform != null)).ThenInclude(p => p.Platform)
            .AsSplitQuery()
            .OrderByDescending(x => x.ImdbRating)
            .ThenBy(x => x.Id)
            .ApplyPagination(page, pageSize)
            .ToListAsync(cancellationToken);

        if (episodes.Count == 0)
        {
            return (Array.Empty<StandaloneEpisodeContext>(), totalCount);
        }

        var results = await HydrateSeriesContextsAsync(episodes, cancellationToken);
        return (results, totalCount);
    }

    public async Task<StandaloneEpisodeContext?> GetStandaloneEpisodeByIdWithSeriesContextAsync(
        Guid episodeId,
        CancellationToken cancellationToken = default)
    {
        var episode = await _context.Episodes
            .AsNoTracking()
            .Where(x => x.Id == episodeId &&
                        x.IsStandalone &&
                        _context.Seasons.Any(s => s.Id == x.SeasonId &&
                                                  _context.Series.Any(sr => sr.Id == s.SeriesId)))
            .Include(x => x.Translations)
            .Include(x => x.EpisodePlatforms.Where(ep => ep.Platform != null)).ThenInclude(p => p.Platform)
            .AsSplitQuery()
            .FirstOrDefaultAsync(cancellationToken);

        if (episode is null)
        {
            return null;
        }

        var contexts = await HydrateSeriesContextsAsync([episode], cancellationToken);
        return contexts.Count > 0 ? contexts[0] : null;
    }

    private async Task<List<StandaloneEpisodeContext>> HydrateSeriesContextsAsync(
        List<Episode> episodes,
        CancellationToken cancellationToken)
    {
        var seasonIds = episodes.Select(e => e.SeasonId).Distinct().ToList();
        var seasonsById = await _context.Seasons
            .AsNoTracking()
            .Where(s => seasonIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, cancellationToken);

        var seriesIds = seasonsById.Values.Select(s => s.SeriesId).Distinct().ToList();
        var seriesById = await _context.Series
            .AsNoTracking()
            .Where(sr => seriesIds.Contains(sr.Id))
            .Include(sr => sr.Translations)
            .Include(sr => sr.MediaPlatforms.Where(mp => mp.Platform != null)).ThenInclude(mp => mp.Platform)
            .Include(sr => sr.MediaMoods.Where(mm => mm.Mood != null))
            .Include(sr => sr.MediaGenres.Where(mg => mg.Genre != null))
            .AsSplitQuery()
            .ToDictionaryAsync(sr => sr.Id, cancellationToken);

        var results = new List<StandaloneEpisodeContext>(episodes.Count);
        foreach (var episode in episodes)
        {
            if (!seasonsById.TryGetValue(episode.SeasonId, out var season))
                continue;

            if (!seriesById.TryGetValue(season.SeriesId, out var series))
                continue;

            results.Add(new StandaloneEpisodeContext(episode, season, series));
        }

        return results;
    }

    private static int NormalizeStandaloneLimit(int? limit) =>
        limit is > 0
            ? Math.Min(limit.Value, MaxStandaloneLimit)
            : DefaultStandaloneLimit;
}
