using Microsoft.EntityFrameworkCore;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Repositories;

namespace ShuffleSeries.Catalog.Infrastructure.Persistence.Repositories;

internal sealed class MoodRepository : IMoodRepository
{
    private readonly CatalogDbContext _context;

    public MoodRepository(CatalogDbContext context)
    {
        _context = context;
    }

    public void Add(Mood mood) => _context.Moods.Add(mood);
    public void Update(Mood mood) => _context.Moods.Update(mood);
    public void Delete(Mood mood) => _context.Moods.Remove(mood);

    public async Task<Mood?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Moods.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Mood?> GetByIdWithTranslationsAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Moods
            .Include(x => x.Translations)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Mood?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        return await _context.Moods
            .Include(x => x.Translations)
            .FirstOrDefaultAsync(x => x.Code == code.Trim().ToLowerInvariant(), cancellationToken);
    }

    public async Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        return await _context.Moods
            .AnyAsync(x => x.Code == code.Trim().ToLowerInvariant(), cancellationToken);
    }

    public async Task<IReadOnlyList<Mood>> GetActiveMoodsWithTranslationsAsync(CancellationToken cancellationToken = default) =>
        await _context.Moods
            .AsNoTracking()
            .Include(x => x.Translations)
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync(cancellationToken);
}
