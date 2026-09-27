using MediatR;
using Moq;
using ShuffleSeries.Shared.Core.Application.Events;
using ShuffleSeries.Shared.Core.Application.Extensions;
using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.Shared.Core.Tests.Application.Extensions;

public class MediatorExtensionsTests
{
    private record SampleDomainEvent(string EventName) : IDomainEvent;

    [Fact]
    public async Task PublishDomainEventAsync_ShouldWrapInDomainEventNotificationAndPublish()
    {
        // Arrange
        var mockPublisher = new Mock<IPublisher>();
        var domainEvent = new SampleDomainEvent("SeriesCreated");

        mockPublisher.Setup(p => p.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await mockPublisher.Object.PublishDomainEventAsync(domainEvent, CancellationToken.None);

        // Assert
        mockPublisher.Verify(p => p.Publish(
            It.Is<INotification>(n => n is DomainEventNotification<SampleDomainEvent> &&
                                      ((DomainEventNotification<SampleDomainEvent>)n).Event == domainEvent),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void DomainEventNotification_ShouldStoreDomainEventProperly()
    {
        var domainEvent = new SampleDomainEvent("TestEvent");
        var notification = new DomainEventNotification<SampleDomainEvent>(domainEvent);

        notification.Event.Should().Be(domainEvent);
    }
}
