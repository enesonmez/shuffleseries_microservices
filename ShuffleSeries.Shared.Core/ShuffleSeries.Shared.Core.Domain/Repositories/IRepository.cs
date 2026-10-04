using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.Shared.Core.Domain.Repositories;

/// <summary>
/// Tüm mikroservis repository arayüzleri için ortak marker interface.
/// Assembly scanning (Scrutor), mimari testler ve cross-cutting interceptor'lar için kullanılır.
/// </summary>
public interface IRepository
{
}

/// <summary>
/// Yalnızca Aggregate Root'lar için geçerli generic repository sözleşmesi.
/// </summary>
public interface IRepository<TEntity> : IRepository where TEntity : AggregateRoot
{
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    void Add(TEntity entity);
    void Update(TEntity entity);
    void Delete(TEntity entity);
}
