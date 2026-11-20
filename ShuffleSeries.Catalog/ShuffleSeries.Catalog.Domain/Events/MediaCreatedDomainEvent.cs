using ShuffleSeries.Catalog.Domain.Enums;
using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.Catalog.Domain.Events;

public sealed record MediaCreatedDomainEvent(Guid MediaId, MediaType MediaType) : IDomainEvent;
