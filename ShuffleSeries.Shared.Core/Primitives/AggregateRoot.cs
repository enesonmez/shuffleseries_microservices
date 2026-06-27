namespace ShuffleSeries.Shared.Core.Primitives;

public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyCollection<IDomainEvent> GetDomainEvents() => _domainEvents.AsReadOnly();
    
    protected AggregateRoot(Guid id) : base(id) {}
    
    protected AggregateRoot() {}
    
    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    protected void ClearDomainEvents() => _domainEvents.Clear();
}