using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.Identity.Domain.Events;

public sealed record UserRegisteredDomainEvent(Guid UserId, string Email, bool IsGuest) : IDomainEvent;
