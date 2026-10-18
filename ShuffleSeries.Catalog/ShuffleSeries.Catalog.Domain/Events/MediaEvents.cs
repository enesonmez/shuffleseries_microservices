using ShuffleSeries.Catalog.Domain.Enums;
using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.Catalog.Domain.Events;

public sealed record MediaCreatedDomainEvent(Guid MediaId, MediaType MediaType) : IDomainEvent;

public sealed record MediaUpdatedDomainEvent(Guid MediaId, MediaType MediaType) : IDomainEvent;

public sealed record MediaDeletedDomainEvent(Guid MediaId, MediaType MediaType) : IDomainEvent;
