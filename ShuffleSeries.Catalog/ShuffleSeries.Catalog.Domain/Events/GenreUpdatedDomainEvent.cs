using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.Catalog.Domain.Events;

public sealed record GenreUpdatedDomainEvent(Guid GenreId) : IDomainEvent;
