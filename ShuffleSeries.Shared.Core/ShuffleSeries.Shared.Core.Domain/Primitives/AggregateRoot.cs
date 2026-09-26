namespace ShuffleSeries.Shared.Core.Domain.Primitives;

/// <summary>
/// Belirli bir ID tipine sahip jenerik DDD Aggregate Root temel sınıfı.
/// </summary>
/// <typeparam name="TId">Varlık kimlik tipi (Guid, string, int vb.).</typeparam>
public abstract class AggregateRoot<TId> : BaseEntity<TId>, IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyCollection<IDomainEvent> GetDomainEvents() => _domainEvents.AsReadOnly();

    protected AggregateRoot(TId id) : base(id) { }

    protected AggregateRoot() { }

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}

/// <summary>
/// Guid tipinde birincil anahtara sahip varsayılan DDD Aggregate Root temel sınıfı.
/// </summary>
public abstract class AggregateRoot : AggregateRoot<Guid>
{
    protected AggregateRoot(Guid id) : base(id) { }

    protected AggregateRoot() { }
}