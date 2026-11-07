using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.Shared.Core.Domain.Events;

public sealed record EpisodeDeletedEvent(
    Guid Id,
    Guid EpisodeId,
    DateTime OccurredOnUtc,
    string? CorrelationId = null) : IIntegrationEvent
{
    public string EventType => nameof(EpisodeDeletedEvent);
}
