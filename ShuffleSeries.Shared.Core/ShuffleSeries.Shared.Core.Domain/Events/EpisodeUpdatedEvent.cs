using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.Shared.Core.Domain.Events;

public sealed record EpisodeUpdatedEvent(
    Guid Id,
    Guid EpisodeId,
    int EpisodeNumber,
    bool IsStandalone,
    int? RuntimeMinutes,
    double? ImdbRating,
    DateTime OccurredOnUtc,
    string? CorrelationId = null) : IIntegrationEvent
{
    public string EventType => nameof(EpisodeUpdatedEvent);
}
