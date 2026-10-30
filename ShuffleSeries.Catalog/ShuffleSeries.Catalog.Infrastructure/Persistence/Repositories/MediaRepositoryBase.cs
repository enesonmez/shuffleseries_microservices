using Microsoft.EntityFrameworkCore;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Infrastructure.Persistence.Repositories;

internal abstract class MediaRepositoryBase<TEntity> : IRepository<TEntity> where TEntity : Media
{
    protected CatalogDbContext Context { get; }

    protected MediaRepositoryBase(CatalogDbContext context)
    {
        Context = context;
    }

    public void Add(TEntity entity) => Context.Set<TEntity>().Add(entity);
    public void Update(TEntity entity)
    {
        if (Context.Entry(entity).State == EntityState.Detached)
        {
            Context.Set<TEntity>().Update(entity);
        }
    }
    public void Delete(TEntity entity) => Context.Set<TEntity>().Remove(entity);

    public virtual async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await Context.Set<TEntity>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<bool> ExistsByTmdbIdAsync(int tmdbId, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        if (tmdbId <= 0)
            return false;

        return await Context.Media
            .AnyAsync(m => m.TmdbId == tmdbId && (!excludeId.HasValue || m.Id != excludeId.Value), cancellationToken);
    }

    public async Task<bool> ExistsByImdbIdAsync(string imdbId, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imdbId))
            return false;

        var normalized = imdbId.Trim();

        return await Context.Media
            .AnyAsync(m => m.ImdbId == normalized && (!excludeId.HasValue || m.Id != excludeId.Value), cancellationToken);
    }
}
