using ShuffleSeries.Shared.Core.Domain.Inbox;

namespace ShuffleSeries.Shared.Core.Tests.Domain.Inbox;

public class InboxMessageTests
{
    [Fact]
    public void Constructor_ShouldInitializePropertiesCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var eventType = "MediaCreatedEvent";
        var content = "{\"id\":\"123\"}";
        var occurredOnUtc = DateTime.UtcNow.AddMinutes(-5);
        var correlationId = "corr-123";

        // Act
        var message = new InboxMessage(id, eventType, content, occurredOnUtc, correlationId);

        // Assert
        message.Id.Should().Be(id);
        message.EventType.Should().Be(eventType);
        message.Content.Should().Be(content);
        message.OccurredOnUtc.Should().Be(occurredOnUtc);
        message.CorrelationId.Should().Be(correlationId);
        message.ReceivedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        message.ProcessedAtUtc.Should().BeNull();
        message.IsProcessed.Should().BeFalse();
        message.Error.Should().BeNull();
    }

    [Fact]
    public void MarkAsProcessed_ShouldSetProcessedAtUtcAndClearError()
    {
        // Arrange
        var message = new InboxMessage(Guid.NewGuid(), "Event", "{}", DateTime.UtcNow);
        message.MarkAsFailed("Transient error");
        var processedAtUtc = DateTime.UtcNow;

        // Act
        message.MarkAsProcessed(processedAtUtc);

        // Assert
        message.IsProcessed.Should().BeTrue();
        message.ProcessedAtUtc.Should().Be(processedAtUtc);
        message.Error.Should().BeNull();
    }

    [Fact]
    public void MarkAsFailed_ShouldSetErrorMessage()
    {
        // Arrange
        var message = new InboxMessage(Guid.NewGuid(), "Event", "{}", DateTime.UtcNow);

        // Act
        message.MarkAsFailed("Deadlock error");

        // Assert
        message.IsProcessed.Should().BeFalse();
        message.Error.Should().Be("Deadlock error");
    }
}
