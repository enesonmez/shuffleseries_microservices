using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Quartz;
using ShuffleSeries.Identity.Domain.Events;
using ShuffleSeries.Identity.Infrastructure.BackgroundJobs;
using ShuffleSeries.Identity.Infrastructure.Persistence;
using ShuffleSeries.Shared.Core.Domain.Events;
using ShuffleSeries.Shared.Core.Domain.Outbox;
using Wolverine;

namespace ShuffleSeries.Identity.Tests.Infrastructure.BackgroundJobs;

public class ProcessOutboxMessagesJobTests
{
    private static IdentityDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new IdentityDbContext(options);
    }

    [Fact]
    public async Task Execute_WhenNoMessages_ShouldDoNothingAndNotPublish()
    {
        // Arrange
        await using var dbContext = CreateInMemoryDbContext();
        var mockPublish = new Mock<IMessageBus>();
        var mockContext = new Mock<IJobExecutionContext>();
        var mockLogger = new Mock<ILogger<ProcessOutboxMessagesJob>>();
        mockContext.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        var job = new ProcessOutboxMessagesJob(dbContext, mockPublish.Object, TimeProvider.System, mockLogger.Object);

        // Act
        await job.Execute(mockContext.Object);

        // Assert
        mockPublish.Verify(p => p.PublishAsync(It.IsAny<object>(), It.IsAny<DeliveryOptions>()), Times.Never);
    }

    [Fact]
    public async Task Execute_WhenUserRegisteredDomainEvent_ShouldPublishUserRegisteredIntegrationEventAndMarkProcessed()
    {
        // Arrange
        await using var dbContext = CreateInMemoryDbContext();
        var mockPublish = new Mock<IMessageBus>();
        var mockContext = new Mock<IJobExecutionContext>();
        var mockLogger = new Mock<ILogger<ProcessOutboxMessagesJob>>();
        mockContext.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        var userId = Guid.NewGuid();
        var domainEvent = new UserRegisteredDomainEvent(userId, "test@shuffleseries.com", IsGuest: false);
        var outboxMessageId = Guid.NewGuid();
        var occurredAt = DateTime.UtcNow.AddMinutes(-1);

        var message = new OutboxMessage
        {
            Id = outboxMessageId,
            Type = nameof(UserRegisteredDomainEvent),
            Content = JsonSerializer.Serialize(domainEvent),
            OccurredOnUtc = occurredAt,
            RetryCount = 0
        };

        dbContext.OutboxMessages.Add(message);
        await dbContext.SaveChangesAsync();

        var job = new ProcessOutboxMessagesJob(dbContext, mockPublish.Object, TimeProvider.System, mockLogger.Object);

        // Act
        await job.Execute(mockContext.Object);

        // Assert
        mockPublish.Verify(p => p.PublishAsync(
            It.Is<UserRegisteredEvent>(e =>
                e.Id == outboxMessageId &&
                e.UserId == userId &&
                e.Email == "test@shuffleseries.com" &&
                !e.IsGuest &&
                e.OccurredOnUtc == occurredAt),
            It.IsAny<DeliveryOptions>()),
            Times.Once);

        var updated = await dbContext.OutboxMessages.FirstAsync(m => m.Id == outboxMessageId);
        updated.ProcessedOnUtc.Should().NotBeNull();
        updated.Error.Should().BeNull();
    }

    [Fact]
    public async Task Execute_WhenUserAccountDeletedDomainEvent_ShouldPublishUserAccountDeletedIntegrationEventAndMarkProcessed()
    {
        // Arrange
        await using var dbContext = CreateInMemoryDbContext();
        var mockPublish = new Mock<IMessageBus>();
        var mockContext = new Mock<IJobExecutionContext>();
        var mockLogger = new Mock<ILogger<ProcessOutboxMessagesJob>>();
        mockContext.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        var userId = Guid.NewGuid();
        var domainEvent = new UserAccountDeletedDomainEvent(userId, "deleted@shuffleseries.com");
        var outboxMessageId = Guid.NewGuid();
        var occurredAt = DateTime.UtcNow.AddMinutes(-3);

        var message = new OutboxMessage
        {
            Id = outboxMessageId,
            Type = nameof(UserAccountDeletedDomainEvent),
            Content = JsonSerializer.Serialize(domainEvent),
            OccurredOnUtc = occurredAt,
            RetryCount = 0
        };

        dbContext.OutboxMessages.Add(message);
        await dbContext.SaveChangesAsync();

        var job = new ProcessOutboxMessagesJob(dbContext, mockPublish.Object, TimeProvider.System, mockLogger.Object);

        // Act
        await job.Execute(mockContext.Object);

        // Assert
        mockPublish.Verify(p => p.PublishAsync(
            It.Is<UserAccountDeletedEvent>(e =>
                e.Id == outboxMessageId &&
                e.UserId == userId &&
                e.Email == "deleted@shuffleseries.com" &&
                e.OccurredOnUtc == occurredAt),
            It.IsAny<DeliveryOptions>()),
            Times.Once);

        var updated = await dbContext.OutboxMessages.FirstAsync(m => m.Id == outboxMessageId);
        updated.ProcessedOnUtc.Should().NotBeNull();
        updated.Error.Should().BeNull();
    }

    [Fact]
    public async Task Execute_WhenUnknownEventType_ShouldIncrementRetryCountAndSetError()
    {
        // Arrange
        await using var dbContext = CreateInMemoryDbContext();
        var mockPublish = new Mock<IMessageBus>();
        var mockContext = new Mock<IJobExecutionContext>();
        var mockLogger = new Mock<ILogger<ProcessOutboxMessagesJob>>();
        mockContext.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = "NonExistentUnknownDomainEvent",
            Content = "{}",
            OccurredOnUtc = DateTime.UtcNow,
            RetryCount = 0
        };

        dbContext.OutboxMessages.Add(message);
        await dbContext.SaveChangesAsync();

        var job = new ProcessOutboxMessagesJob(dbContext, mockPublish.Object, TimeProvider.System, mockLogger.Object);

        // Act
        await job.Execute(mockContext.Object);

        // Assert
        mockPublish.Verify(p => p.PublishAsync(It.IsAny<object>(), It.IsAny<DeliveryOptions>()), Times.Never);

        var updated = await dbContext.OutboxMessages.FirstAsync(m => m.Id == message.Id);
        updated.ProcessedOnUtc.Should().BeNull();
        updated.RetryCount.Should().Be(1);
        updated.Error.Should().Contain("Not found event type");
    }

    [Fact]
    public async Task Execute_WhenContentInvalidJson_ShouldIncrementRetryCountAndSetError()
    {
        // Arrange
        await using var dbContext = CreateInMemoryDbContext();
        var mockPublish = new Mock<IMessageBus>();
        var mockContext = new Mock<IJobExecutionContext>();
        var mockLogger = new Mock<ILogger<ProcessOutboxMessagesJob>>();
        mockContext.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = nameof(UserRegisteredDomainEvent),
            Content = "invalid-not-json-content",
            OccurredOnUtc = DateTime.UtcNow,
            RetryCount = 0
        };

        dbContext.OutboxMessages.Add(message);
        await dbContext.SaveChangesAsync();

        var job = new ProcessOutboxMessagesJob(dbContext, mockPublish.Object, TimeProvider.System, mockLogger.Object);

        // Act
        await job.Execute(mockContext.Object);

        // Assert
        mockPublish.Verify(p => p.PublishAsync(It.IsAny<object>(), It.IsAny<DeliveryOptions>()), Times.Never);

        var updated = await dbContext.OutboxMessages.FirstAsync(m => m.Id == message.Id);
        updated.ProcessedOnUtc.Should().BeNull();
        updated.RetryCount.Should().Be(1);
        updated.Error.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Execute_WhenPublishThrowsException_ShouldIncrementRetryCountAndSetError()
    {
        // Arrange
        await using var dbContext = CreateInMemoryDbContext();
        var mockPublish = new Mock<IMessageBus>();
        var mockContext = new Mock<IJobExecutionContext>();
        var mockLogger = new Mock<ILogger<ProcessOutboxMessagesJob>>();
        mockContext.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        mockPublish.Setup(p => p.PublishAsync(It.IsAny<UserRegisteredEvent>(), It.IsAny<DeliveryOptions>()))
            .ThrowsAsync(new InvalidOperationException("RabbitMQ connection down"));

        var domainEvent = new UserRegisteredDomainEvent(Guid.NewGuid(), "test@shuffleseries.com", IsGuest: false);
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = nameof(UserRegisteredDomainEvent),
            Content = JsonSerializer.Serialize(domainEvent),
            OccurredOnUtc = DateTime.UtcNow,
            RetryCount = 1
        };

        dbContext.OutboxMessages.Add(message);
        await dbContext.SaveChangesAsync();

        var job = new ProcessOutboxMessagesJob(dbContext, mockPublish.Object, TimeProvider.System, mockLogger.Object);

        // Act
        await job.Execute(mockContext.Object);

        // Assert
        var updated = await dbContext.OutboxMessages.FirstAsync(m => m.Id == message.Id);
        updated.ProcessedOnUtc.Should().BeNull();
        updated.RetryCount.Should().Be(2);
        updated.Error.Should().Be("RabbitMQ connection down");
    }

    [Fact]
    public async Task Execute_WhenMessageRetryCountIsAtMaxRetries_ShouldSkipPoisonMessage()
    {
        // Arrange
        await using var dbContext = CreateInMemoryDbContext();
        var mockPublish = new Mock<IMessageBus>();
        var mockContext = new Mock<IJobExecutionContext>();
        var mockLogger = new Mock<ILogger<ProcessOutboxMessagesJob>>();
        mockContext.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        var domainEvent = new UserRegisteredDomainEvent(Guid.NewGuid(), "poison@shuffleseries.com", IsGuest: false);
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = nameof(UserRegisteredDomainEvent),
            Content = JsonSerializer.Serialize(domainEvent),
            OccurredOnUtc = DateTime.UtcNow,
            RetryCount = ProcessOutboxMessagesJob.MaxRetries, // Already at max retries
            Error = "Previous permanent error"
        };

        dbContext.OutboxMessages.Add(message);
        await dbContext.SaveChangesAsync();

        var job = new ProcessOutboxMessagesJob(dbContext, mockPublish.Object, TimeProvider.System, mockLogger.Object);

        // Act
        await job.Execute(mockContext.Object);

        // Assert
        mockPublish.Verify(p => p.PublishAsync(It.IsAny<object>(), It.IsAny<DeliveryOptions>()), Times.Never);

        var unchanged = await dbContext.OutboxMessages.FirstAsync(m => m.Id == message.Id);
        unchanged.ProcessedOnUtc.Should().BeNull();
        unchanged.RetryCount.Should().Be(ProcessOutboxMessagesJob.MaxRetries);
    }
}
