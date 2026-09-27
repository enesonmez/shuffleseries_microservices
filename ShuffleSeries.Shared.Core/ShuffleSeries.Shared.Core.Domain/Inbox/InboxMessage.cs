using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.Shared.Core.Domain.Inbox;

/// <summary>
/// Represents an incoming integration event recorded in the Inbox table to guarantee idempotency.
/// Prevents the same message from being processed more than once by a consumer.
/// </summary>
public sealed class InboxMessage : BaseEntity<Guid>
{
    private InboxMessage()
    {
    }

    public InboxMessage(
        Guid id,
        string eventType,
        string content,
        DateTime occurredOnUtc,
        string? correlationId = null)
    {
        Id = id;
        EventType = eventType;
        Content = content;
        OccurredOnUtc = occurredOnUtc;
        CorrelationId = correlationId;
        ReceivedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Qualified type of the message/event.
    /// </summary>
    public string EventType { get; private set; } = null!;

    /// <summary>
    /// Serialized JSON payload of the message.
    /// </summary>
    public string Content { get; private set; } = null!;

    /// <summary>
    /// UTC timestamp when the event was produced.
    /// </summary>
    public DateTime OccurredOnUtc { get; private set; }

    /// <summary>
    /// UTC timestamp when the message was received by this service.
    /// </summary>
    public DateTime ReceivedAtUtc { get; private set; }

    /// <summary>
    /// UTC timestamp when the message processing successfully concluded.
    /// </summary>
    public DateTime? ProcessedAtUtc { get; private set; }

    /// <summary>
    /// Correlation identifier for end-to-end tracing.
    /// </summary>
    public string? CorrelationId { get; private set; }

    /// <summary>
    /// Error message if processing failed.
    /// </summary>
    public string? Error { get; private set; }

    /// <summary>
    /// Whether the message has been processed successfully.
    /// </summary>
    public bool IsProcessed => ProcessedAtUtc.HasValue;

    public void MarkAsProcessed(DateTime processedAtUtc)
    {
        ProcessedAtUtc = processedAtUtc;
        Error = null;
    }

    public void MarkAsFailed(string error) => Error = error;
}
