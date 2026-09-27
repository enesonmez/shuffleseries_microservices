using Microsoft.EntityFrameworkCore;
using ShuffleSeries.Shared.Core.Domain.Outbox;
using ShuffleSeries.Shared.Core.Domain.Primitives;
using ShuffleSeries.Shared.Core.Infrastructure.Interceptors;

namespace ShuffleSeries.Shared.Core.Tests.Infrastructure.Interceptors;

public class InsertOutboxMessagesInterceptorTests
{
    private record SampleDomainEvent(string EventName) : IDomainEvent;

    private sealed class DummyAggregate : AggregateRoot
    {
        public DummyAggregate(Guid id) : base(id) { }
        private DummyAggregate() { }

        public void Emit(string name) => RaiseDomainEvent(new SampleDomainEvent(name));
    }

    private sealed class TestOutboxDbContext : DbContext
    {
        public DbSet<DummyAggregate> Aggregates => Set<DummyAggregate>();
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

        public TestOutboxDbContext(DbContextOptions<TestOutboxDbContext> options) : base(options) { }
    }

    private static TestOutboxDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TestOutboxDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(new InsertOutboxMessagesInterceptor())
            .Options;

        return new TestOutboxDbContext(options);
    }

    [Fact]
    public async Task SavingChangesAsync_WhenDomainEventsPresent_ShouldQueueOutboxMessages()
    {
        await using var context = CreateDbContext();
        var aggregate = new DummyAggregate(Guid.NewGuid());
        aggregate.Emit("OrderPlaced");

        context.Aggregates.Add(aggregate);
        await context.SaveChangesAsync();

        var outboxEntries = context.ChangeTracker.Entries<OutboxMessage>().ToList();
        outboxEntries.Should().HaveCount(1);
        outboxEntries.First().Entity.Type.Should().Be(nameof(SampleDomainEvent));
        outboxEntries.First().Entity.Content.Should().Contain("OrderPlaced");
        aggregate.GetDomainEvents().Should().BeEmpty();
    }

    [Fact]
    public void SavingChanges_Sync_WhenDomainEventsPresent_ShouldQueueOutboxMessages()
    {
        using var context = CreateDbContext();
        var aggregate = new DummyAggregate(Guid.NewGuid());
        aggregate.Emit("ItemCreated");

        context.Aggregates.Add(aggregate);
        context.SaveChanges();

        var outboxEntries = context.ChangeTracker.Entries<OutboxMessage>().ToList();
        outboxEntries.Should().HaveCount(1);
        outboxEntries.First().Entity.Type.Should().Be(nameof(SampleDomainEvent));
        aggregate.GetDomainEvents().Should().BeEmpty();
    }

    [Fact]
    public async Task SavingChangesAsync_WhenNoDomainEvents_ShouldNotQueueOutboxMessages()
    {
        await using var context = CreateDbContext();
        var aggregate = new DummyAggregate(Guid.NewGuid());

        context.Aggregates.Add(aggregate);
        await context.SaveChangesAsync();

        var outboxEntries = context.ChangeTracker.Entries<OutboxMessage>().ToList();
        outboxEntries.Should().BeEmpty();
    }
}
