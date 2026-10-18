using Microsoft.EntityFrameworkCore;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Repositories;

namespace ShuffleSeries.Catalog.Infrastructure.Persistence.Repositories;

internal sealed class MediaRepository : IMediaRepository
{
    private readonly CatalogDbContext _context;

    public MediaRepository(CatalogDbContext context)
    {
        _context = context;
    }

    public void Add(Media media) => _context.Media.Add(media);
    public void Update(Media media) => _context.Media.Update(media);
    public void Delete(Media media) => _context.Media.Remove(media);

    public async Task<Media?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Media.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Media?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Media
            .Include(x => x.Translations)
            .Include(x => x.MediaPlatforms).ThenInclude(p => p.Platform)
            .Include(x => x.MediaMoods).ThenInclude(m => m.Mood)
            .Include(x => x.MediaGenres).ThenInclude(g => g.Genre)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<bool> ExistsByImdbIdAsync(string imdbId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imdbId))
            return false;

        return await _context.Media.AnyAsync(x => x.ImdbId == imdbId.Trim(), cancellationToken);
    }
}
