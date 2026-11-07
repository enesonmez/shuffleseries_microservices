using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.Shared.Core.Domain.Events;

public sealed record MediaDeletedEvent(
    Guid Id,
    Guid MediaId,
    string MediaType,
    DateTime OccurredOnUtc,
    string? CorrelationId = null) : IIntegrationEvent
{
    public string EventType => nameof(MediaDeletedEvent);
}
