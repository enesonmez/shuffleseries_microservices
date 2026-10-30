using Microsoft.EntityFrameworkCore;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Repositories;

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
            .Include(x => x.EpisodePlatforms).ThenInclude(p => p.Platform)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Episode>> GetStandaloneEpisodesAsync(int? limit = null, CancellationToken cancellationToken = default)
    {
        var effectiveLimit = limit is > 0
            ? Math.Min(limit.Value, MaxStandaloneLimit)
            : DefaultStandaloneLimit;

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
}
