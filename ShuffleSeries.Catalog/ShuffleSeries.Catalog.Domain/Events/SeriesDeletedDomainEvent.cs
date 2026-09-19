using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.Catalog.Domain.Events;

public record SeriesDeletedDomainEvent(Guid SeriesId) : IDomainEvent;