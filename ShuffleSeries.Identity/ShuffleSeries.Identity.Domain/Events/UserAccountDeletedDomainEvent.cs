using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.Identity.Domain.Events;

public sealed record UserAccountDeletedDomainEvent(Guid UserId, string? Email) : IDomainEvent;
