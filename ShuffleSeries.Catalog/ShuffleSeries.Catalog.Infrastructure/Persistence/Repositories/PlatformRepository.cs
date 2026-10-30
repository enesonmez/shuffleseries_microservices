using Microsoft.EntityFrameworkCore;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Repositories;

namespace ShuffleSeries.Catalog.Infrastructure.Persistence.Repositories;

internal sealed class PlatformRepository : IPlatformRepository
{
    private readonly CatalogDbContext _context;

    public PlatformRepository(CatalogDbContext context)
    {
        _context = context;
    }

    public void Add(Platform platform) => _context.Platforms.Add(platform);
    public void Update(Platform platform) => _context.Platforms.Update(platform);
    public void Delete(Platform platform) => _context.Platforms.Remove(platform);

    public async Task<Platform?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Platforms.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Platform?> GetByIdForDetailAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Platforms
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return false;

        return await _context.Platforms
            .AnyAsync(x => x.Slug == slug.Trim().ToLowerInvariant(), cancellationToken);
    }

    public async Task<IReadOnlyList<Platform>> GetActivePlatformsAsync(CancellationToken cancellationToken = default) =>
        await _context.Platforms
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
}
