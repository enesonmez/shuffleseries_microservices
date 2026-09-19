using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.Catalog.Domain.Events;

public record SeriesUpdatedDomainEvent(Guid SeriesId) : IDomainEvent;