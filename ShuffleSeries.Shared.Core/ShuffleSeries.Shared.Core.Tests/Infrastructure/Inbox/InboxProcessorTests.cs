using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ShuffleSeries.Shared.Core.Domain.Events;
using ShuffleSeries.Shared.Core.Domain.Inbox;
using ShuffleSeries.Shared.Core.Infrastructure.Inbox;
using ShuffleSeries.Shared.Core.Infrastructure.Persistence.Configurations;

namespace ShuffleSeries.Shared.Core.Tests.Infrastructure.Inbox;

public class InboxProcessorTests
{
    private sealed class TestInboxDbContext(DbContextOptions<TestInboxDbContext> options) : DbContext(options)
    {
        public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder) =>
            modelBuilder.ApplyConfiguration(new InboxMessageConfiguration());
    }

    private static TestInboxDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestInboxDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TestInboxDbContext(options);
    }

    [Fact]
    public async Task ExecuteIdempotentAsync_WhenNewEvent_ShouldInvokeActionAndMarkProcessed_AndSkipSecondCall()
    {
        await using var context = CreateContext();
        var evt = new MediaDeletedEvent(Guid.NewGuid(), Guid.NewGuid(), "Movie", DateTime.UtcNow, "corr-1");
        var callCount = 0;

        var firstProcessed = await InboxProcessor.ExecuteIdempotentAsync(
            context,
            evt,
            TimeProvider.System,
            NullLogger.Instance,
            _ =>
            {
                callCount++;
                return Task.CompletedTask;
            });

        var secondProcessed = await InboxProcessor.ExecuteIdempotentAsync(
            context,
            evt,
            TimeProvider.System,
            NullLogger.Instance,
            _ =>
            {
                callCount++;
                return Task.CompletedTask;
            });

        firstProcessed.Should().BeTrue();
        secondProcessed.Should().BeFalse();
        callCount.Should().Be(1);

        var stored = await context.InboxMessages.SingleAsync(x => x.Id == evt.Id);
        stored.IsProcessed.Should().BeTrue();
        stored.Error.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteIdempotentAsync_WhenActionThrows_ShouldRecordErrorAndAllowRetryToSucceed()
    {
        await using var context = CreateContext();
        var evt = new EpisodeDeletedEvent(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);

        var act = async () => await InboxProcessor.ExecuteIdempotentAsync(
            context,
            evt,
            TimeProvider.System,
            NullLogger.Instance,
            _ => throw new InvalidOperationException("Transient Redis timeout"));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Transient Redis timeout*");

        var failedRecord = await context.InboxMessages.SingleAsync(x => x.Id == evt.Id);
        failedRecord.IsProcessed.Should().BeFalse();
        failedRecord.Error.Should().Contain("Transient Redis timeout");

        var retrySucceeded = await InboxProcessor.ExecuteIdempotentAsync(
            context,
            evt,
            TimeProvider.System,
            NullLogger.Instance,
            _ => Task.CompletedTask);

        retrySucceeded.Should().BeTrue();
        var recoveredRecord = await context.InboxMessages.SingleAsync(x => x.Id == evt.Id);
        recoveredRecord.IsProcessed.Should().BeTrue();
        recoveredRecord.Error.Should().BeNull();
    }

    [Fact]
    public async Task PurgeProcessedMessagesAsync_OnNonRelationalProvider_ShouldReturnSafely()
    {
        await using var context = CreateContext();
        var act = async () => await InboxProcessor.PurgeProcessedMessagesAsync(
            context,
            TimeProvider.System,
            retentionDays: 7,
            NullLogger.Instance);

        await act.Should().NotThrowAsync();
    }
}
