using Microsoft.EntityFrameworkCore;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Repositories;

namespace ShuffleSeries.Catalog.Infrastructure.Persistence.Repositories;

internal sealed class MovieRepository : MediaRepositoryBase<Movie>, IMovieRepository
{
    public MovieRepository(CatalogDbContext context) : base(context)
    {
    }

    public async Task<Movie?> GetByIdForDetailAsync(Guid id, CancellationToken cancellationToken = default) =>
        await Context.Movies
            .AsNoTracking()
            .Include(x => x.Translations)
            .Include(x => x.MediaPlatforms).ThenInclude(p => p.Platform)
            .Include(x => x.MediaMoods).ThenInclude(m => m.Mood).ThenInclude(m => m!.Translations)
            .Include(x => x.MediaGenres).ThenInclude(g => g.Genre).ThenInclude(g => g!.Translations)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Movie?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        await Context.Movies
            .Include(x => x.Translations)
            .Include(x => x.MediaPlatforms)
            .Include(x => x.MediaMoods)
            .Include(x => x.MediaGenres)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
}
