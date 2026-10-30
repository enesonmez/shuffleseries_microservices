namespace ShuffleSeries.Shared.Core.Domain.Primitives;

/// <summary>
/// Domain Event üretebilen ve ChangeTracker / Outbox mekanizması tarafından taranabilen varlık sözleşmesi.
/// Hem Aggregate Root'lar hem de alt varlıklar (Child Entities) bu arayüzü uygulayabilir.
/// </summary>
public interface IHaveDomainEvents
{
    IReadOnlyCollection<IDomainEvent> GetDomainEvents();
    void ClearDomainEvents();
}
