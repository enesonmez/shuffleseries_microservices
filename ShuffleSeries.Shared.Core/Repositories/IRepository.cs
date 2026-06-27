using ShuffleSeries.Shared.Core.Primitives;

namespace ShuffleSeries.Shared.Core.Repositories;

public interface IRepository<TEntity> where TEntity : AggregateRoot
{
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    void Add(TEntity entity);
    void Update(TEntity entity);
    void Delete(TEntity entity);
}