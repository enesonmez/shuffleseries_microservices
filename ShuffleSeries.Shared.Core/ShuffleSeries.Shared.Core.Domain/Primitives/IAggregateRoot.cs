namespace ShuffleSeries.Shared.Core.Domain.Primitives;

/// <summary>
/// Domain-Driven Design (DDD) Aggregate Root sözleşmesi.
/// Domain event'lerin toplanması ve temizlenmesi işlevlerini soyutlar.
/// </summary>
public interface IAggregateRoot
{
    IReadOnlyCollection<IDomainEvent> GetDomainEvents();
    void ClearDomainEvents();
}
