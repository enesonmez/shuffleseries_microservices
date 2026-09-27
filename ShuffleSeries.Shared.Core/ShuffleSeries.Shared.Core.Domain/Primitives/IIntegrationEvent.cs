namespace ShuffleSeries.Shared.Core.Domain.Primitives;

/// <summary>
/// Contract representing an event published across bounded context boundaries (microservices).
/// </summary>
public interface IIntegrationEvent
{
    /// <summary>
    /// Unique identifier of the event for idempotency and tracking.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// UTC timestamp of when the event originally occurred.
    /// </summary>
    DateTime OccurredOnUtc { get; }

    /// <summary>
    /// Optional correlation identifier passed across distributed services.
    /// </summary>
    string? CorrelationId { get; }

    /// <summary>
    /// Qualified type name of the event.
    /// </summary>
    string EventType { get; }
}
