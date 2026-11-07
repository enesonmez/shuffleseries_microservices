using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.Shared.Core.Domain.Events;

public sealed record EpisodeCreatedEvent(
    Guid Id,
    Guid SeriesId,
    Guid SeasonId,
    Guid EpisodeId,
    int EpisodeNumber,
    bool IsStandalone,
    DateTime OccurredOnUtc,
    string? CorrelationId = null) : IIntegrationEvent
{
    public string EventType => nameof(EpisodeCreatedEvent);
}
