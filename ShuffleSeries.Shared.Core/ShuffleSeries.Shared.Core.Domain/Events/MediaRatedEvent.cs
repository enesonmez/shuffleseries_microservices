using ShuffleSeries.Shared.Core.Domain.Enums;
using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.Shared.Core.Domain.Events;

/// <summary>
/// Published when a user rates a watched content item using the 3-state feedback model
/// (DoubleThumbsUp +30%, Liked +10%, Disliked -50%).
/// Consumers:
/// - Shuffle Engine: Updates user's Redis affinity feature vector (mood, genre, director weights).
/// - Ticket Economy: Grants +3 bonus tickets every 3 rated items.
/// - History &amp; Analytics: Feeds recommendation telemetry.
/// </summary>
public sealed record MediaRatedEvent(
    Guid Id,
    Guid UserId,
    Guid MediaId,
    MediaRatingTier RatingTier,
    DateTime OccurredOnUtc,
    string? CorrelationId = null) : IIntegrationEvent
{
    public string EventType => nameof(MediaRatedEvent);
}
