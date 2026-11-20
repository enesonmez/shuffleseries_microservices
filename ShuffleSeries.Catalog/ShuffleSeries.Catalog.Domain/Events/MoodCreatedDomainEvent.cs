using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.Catalog.Domain.Events;

public sealed record MoodCreatedDomainEvent(Guid MoodId) : IDomainEvent;
