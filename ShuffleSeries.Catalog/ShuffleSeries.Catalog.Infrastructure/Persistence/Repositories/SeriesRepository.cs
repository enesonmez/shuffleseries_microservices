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
    
    public async Task<Series?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Series.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public void Add(Series series) => _context.Series.Add(series);
    public void Update(Series series) => _context.Series.Update(series);
    public void Delete(Series series) => _context.Series.Remove(series);
}