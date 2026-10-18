using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.Catalog.Domain.Events;

public sealed record GenreCreatedDomainEvent(Guid GenreId) : IDomainEvent;

public sealed record GenreUpdatedDomainEvent(Guid GenreId) : IDomainEvent;

public sealed record GenreDeletedDomainEvent(Guid GenreId) : IDomainEvent;
