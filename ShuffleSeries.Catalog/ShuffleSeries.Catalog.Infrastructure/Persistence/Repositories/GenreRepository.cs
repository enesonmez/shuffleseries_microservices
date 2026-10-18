using Microsoft.EntityFrameworkCore;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Repositories;

namespace ShuffleSeries.Catalog.Infrastructure.Persistence.Repositories;

internal sealed class GenreRepository : IGenreRepository
{
    private readonly CatalogDbContext _context;

    public GenreRepository(CatalogDbContext context)
    {
        _context = context;
    }

    public void Add(Genre genre) => _context.Genres.Add(genre);
    public void Update(Genre genre) => _context.Genres.Update(genre);
    public void Delete(Genre genre) => _context.Genres.Remove(genre);

    public async Task<Genre?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Genres.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Genre?> GetByIdWithTranslationsAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Genres
            .Include(x => x.Translations)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Genre?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        return await _context.Genres
            .Include(x => x.Translations)
            .FirstOrDefaultAsync(x => x.Code == code.Trim().ToLowerInvariant(), cancellationToken);
    }

    public async Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        return await _context.Genres
            .AnyAsync(x => x.Code == code.Trim().ToLowerInvariant(), cancellationToken);
    }

    public async Task<IReadOnlyList<Genre>> GetActiveGenresWithTranslationsAsync(CancellationToken cancellationToken = default) =>
        await _context.Genres
            .AsNoTracking()
            .Include(x => x.Translations)
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync(cancellationToken);
}
