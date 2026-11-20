using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.Shared.Core.Domain.Events;

public sealed record CatalogTaxonomyChangedEvent(
    Guid Id,
    string TaxonomyType,
    Guid ItemId,
    string Action,
    DateTime OccurredOnUtc,
    string? CorrelationId = null) : IIntegrationEvent
{
    public string EventType => nameof(CatalogTaxonomyChangedEvent);
}
