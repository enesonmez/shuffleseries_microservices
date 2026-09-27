using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Moq;
using Quartz;
using ShuffleSeries.Catalog.Domain.Events;
using ShuffleSeries.Catalog.Infrastructure.BackgroundJobs;
using ShuffleSeries.Catalog.Infrastructure.Persistence;
using ShuffleSeries.Shared.Core.Domain.Outbox;

namespace ShuffleSeries.Catalog.Tests.Infrastructure.BackgroundJobs;

public class ProcessOutboxMessagesJobTests
{
    private static CatalogDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new CatalogDbContext(options);
    }

    [Fact]
    public async Task Execute_WhenNoMessages_ShouldDoNothing()
    {
        await using var dbContext = CreateInMemoryDbContext();
        var mockPublish = new Mock<IPublishEndpoint>();
        var mockContext = new Mock<IJobExecutionContext>();
        mockContext.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        var job = new ProcessOutboxMessagesJob(dbContext, mockPublish.Object);

        await job.Execute(mockContext.Object);

        mockPublish.Verify(p => p.Publish(It.IsAny<object>(), It.IsAny<Type>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Execute_WhenValidMessage_ShouldPublishAndMarkProcessed()
    {
        await using var dbContext = CreateInMemoryDbContext();
        var mockPublish = new Mock<IPublishEndpoint>();
        var mockContext = new Mock<IJobExecutionContext>();
        mockContext.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        var seriesId = Guid.NewGuid();
        var domainEvent = new SeriesCreatedDomainEvent(seriesId);
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = nameof(SeriesCreatedDomainEvent),
            Content = JsonSerializer.Serialize(domainEvent),
            OccurredOnUtc = DateTime.UtcNow
        };

        dbContext.OutboxMessages.Add(message);
        await dbContext.SaveChangesAsync();

        var job = new ProcessOutboxMessagesJob(dbContext, mockPublish.Object);

        await job.Execute(mockContext.Object);

        mockPublish.Verify(p => p.Publish(It.IsAny<object>(), It.Is<Type>(t => t == typeof(SeriesCreatedDomainEvent)), It.IsAny<CancellationToken>()), Times.Once);

        var processed = await dbContext.OutboxMessages.FirstAsync(m => m.Id == message.Id);
        processed.ProcessedOnUtc.Should().NotBeNull();
        processed.Error.Should().BeNull();
    }

    [Fact]
    public async Task Execute_WhenUnknownEventType_ShouldRecordError()
    {
        await using var dbContext = CreateInMemoryDbContext();
        var mockPublish = new Mock<IPublishEndpoint>();
        var mockContext = new Mock<IJobExecutionContext>();
        mockContext.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = "NonExistingDomainEvent",
            Content = "{}",
            OccurredOnUtc = DateTime.UtcNow
        };

        dbContext.OutboxMessages.Add(message);
        await dbContext.SaveChangesAsync();

        var job = new ProcessOutboxMessagesJob(dbContext, mockPublish.Object);

        await job.Execute(mockContext.Object);

        var processed = await dbContext.OutboxMessages.FirstAsync(m => m.Id == message.Id);
        processed.ProcessedOnUtc.Should().BeNull();
        processed.Error.Should().Contain("Not found event type");
    }

    [Fact]
    public async Task Execute_WhenPublishThrows_ShouldRecordExceptionMessageInError()
    {
        await using var dbContext = CreateInMemoryDbContext();
        var mockPublish = new Mock<IPublishEndpoint>();
        var mockContext = new Mock<IJobExecutionContext>();
        mockContext.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        mockPublish.Setup(p => p.Publish(It.IsAny<object>(), It.IsAny<Type>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("RabbitMQ connection refused"));

        var seriesId = Guid.NewGuid();
        var domainEvent = new SeriesCreatedDomainEvent(seriesId);
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = nameof(SeriesCreatedDomainEvent),
            Content = JsonSerializer.Serialize(domainEvent),
            OccurredOnUtc = DateTime.UtcNow
        };

        dbContext.OutboxMessages.Add(message);
        await dbContext.SaveChangesAsync();

        var job = new ProcessOutboxMessagesJob(dbContext, mockPublish.Object);

        await job.Execute(mockContext.Object);

        var processed = await dbContext.OutboxMessages.FirstAsync(m => m.Id == message.Id);
        processed.ProcessedOnUtc.Should().BeNull();
        processed.Error.Should().Be("RabbitMQ connection refused");
        processed.RetryCount.Should().Be(1);
    }

    [Fact]
    public async Task Execute_WhenMessageExceedsMaxRetries_ShouldSkipPoisonMessage()
    {
        await using var dbContext = CreateInMemoryDbContext();
        var mockPublish = new Mock<IPublishEndpoint>();
        var mockContext = new Mock<IJobExecutionContext>();
        mockContext.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        var poisonMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = "MalformedEvent",
            Content = "{}",
            OccurredOnUtc = DateTime.UtcNow.AddMinutes(-10),
            RetryCount = 3, // Already reached MaxRetries
            Error = "Previous recurring error"
        };

        var seriesId = Guid.NewGuid();
        var domainEvent = new SeriesCreatedDomainEvent(seriesId);
        var validMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = nameof(SeriesCreatedDomainEvent),
            Content = JsonSerializer.Serialize(domainEvent),
            OccurredOnUtc = DateTime.UtcNow,
            RetryCount = 0
        };

        dbContext.OutboxMessages.AddRange(poisonMessage, validMessage);
        await dbContext.SaveChangesAsync();

        var job = new ProcessOutboxMessagesJob(dbContext, mockPublish.Object);

        await job.Execute(mockContext.Object);

        // The poison message should not be attempted again
        mockPublish.Verify(p => p.Publish(It.IsAny<object>(), It.Is<Type>(t => t == typeof(SeriesCreatedDomainEvent)), It.IsAny<CancellationToken>()), Times.Once);

        var poisonResult = await dbContext.OutboxMessages.FirstAsync(m => m.Id == poisonMessage.Id);
        poisonResult.RetryCount.Should().Be(3);
        poisonResult.ProcessedOnUtc.Should().BeNull();

        var validResult = await dbContext.OutboxMessages.FirstAsync(m => m.Id == validMessage.Id);
        validResult.ProcessedOnUtc.Should().NotBeNull();
        validResult.RetryCount.Should().Be(0);
    }
}
