using Microsoft.EntityFrameworkCore;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Repositories;

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

    public async Task<Series?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Series.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsByTitleAsync(string title, CancellationToken cancellationToken = default)
    {
        return await _context.Series
            .AnyAsync(x => x.Title.ToLower() == title.ToLower(), cancellationToken);
    }

    public async Task<(IReadOnlyList<Series> Items, int TotalCount)> GetPagedListAsync(int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await _context.Series.CountAsync(cancellationToken);
        
        var items = await _context.Series
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        
        return (items, totalCount);
    }
}