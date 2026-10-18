using Microsoft.EntityFrameworkCore;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Repositories;

namespace ShuffleSeries.Catalog.Infrastructure.Persistence.Repositories;

internal sealed class MovieRepository : IMovieRepository
{
    private readonly CatalogDbContext _context;

    public MovieRepository(CatalogDbContext context)
    {
        _context = context;
    }

    public void Add(Movie movie) => _context.Movies.Add(movie);
    public void Update(Movie movie) => _context.Movies.Update(movie);
    public void Delete(Movie movie) => _context.Movies.Remove(movie);

    public async Task<Movie?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Movies.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Movie?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Movies
            .Include(x => x.Translations)
            .Include(x => x.MediaPlatforms).ThenInclude(p => p.Platform)
            .Include(x => x.MediaMoods).ThenInclude(m => m.Mood)
            .Include(x => x.MediaGenres).ThenInclude(g => g.Genre)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
}
