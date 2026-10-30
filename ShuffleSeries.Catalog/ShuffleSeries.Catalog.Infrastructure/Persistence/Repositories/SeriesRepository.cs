using Microsoft.EntityFrameworkCore;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Repositories;

namespace ShuffleSeries.Catalog.Infrastructure.Persistence.Repositories;

internal sealed class SeriesRepository : MediaRepositoryBase<Series>, ISeriesRepository
{
    public SeriesRepository(CatalogDbContext context) : base(context)
    {
    }

    public async Task<Series?> GetByIdWithSeasonsAsync(Guid id, CancellationToken cancellationToken = default) =>
        await Context.Series
            .Include(x => x.Seasons)
                .ThenInclude(s => s.Episodes)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Series?> GetByIdForDetailAsync(Guid id, CancellationToken cancellationToken = default) =>
        await Context.Series
            .AsNoTracking()
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

    public async Task<Series?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        await Context.Series
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
}
