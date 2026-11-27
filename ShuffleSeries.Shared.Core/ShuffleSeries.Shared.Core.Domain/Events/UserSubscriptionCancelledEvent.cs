using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.Shared.Core.Domain.Events;

/// <summary>
/// Published when a user's Premium subscription expires, is cancelled, or is refunded.
/// Consumers:
/// - Identity Service: Downgrades user role from Premium to Standard.
/// - Shuffle Engine: Revokes Redis VIP entitlement back to standard quota.
/// - History &amp; Analytics: Records subscription churn telemetry.
/// </summary>
public sealed record UserSubscriptionCancelledEvent(
    Guid Id,
    Guid UserId,
    DateTime CancelledAtUtc,
    DateTime OccurredOnUtc,
    string? CorrelationId = null) : IIntegrationEvent
{
    public string EventType => nameof(UserSubscriptionCancelledEvent);
}
