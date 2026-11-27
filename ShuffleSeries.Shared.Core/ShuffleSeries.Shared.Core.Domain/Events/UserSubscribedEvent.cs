using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.Shared.Core.Domain.Events;

/// <summary>
/// Published when a user activates or renews a Premium subscription.
/// Consumers:
/// - Identity Service: Promotes user role to Premium and updates VIP claims.
/// - Shuffle Engine: Unlocks unlimited shuffle entitlement and VIP Weekend Arena in Redis.
/// - Notification Service: Sends VIP welcome notification.
/// - History &amp; Analytics: Records subscription conversion telemetry.
/// </summary>
public sealed record UserSubscribedEvent(
    Guid Id,
    Guid UserId,
    string PlanCode,
    DateTime ExpiresAtUtc,
    DateTime OccurredOnUtc,
    string? CorrelationId = null) : IIntegrationEvent
{
    public string EventType => nameof(UserSubscribedEvent);
}
