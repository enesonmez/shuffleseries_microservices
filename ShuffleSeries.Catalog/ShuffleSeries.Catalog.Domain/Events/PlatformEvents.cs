using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.Catalog.Domain.Events;

public sealed record PlatformCreatedDomainEvent(Guid PlatformId) : IDomainEvent;

public sealed record PlatformUpdatedDomainEvent(Guid PlatformId) : IDomainEvent;

public sealed record PlatformDeletedDomainEvent(Guid PlatformId) : IDomainEvent;
