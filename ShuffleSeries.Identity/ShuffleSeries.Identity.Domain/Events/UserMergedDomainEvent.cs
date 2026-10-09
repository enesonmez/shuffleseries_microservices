using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.Identity.Domain.Events;

public sealed record UserMergedDomainEvent(Guid TargetUserId, Guid GuestUserId) : IDomainEvent;
