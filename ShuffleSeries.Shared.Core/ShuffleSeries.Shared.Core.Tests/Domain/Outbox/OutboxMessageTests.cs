using ShuffleSeries.Shared.Core.Domain.Outbox;
using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.Shared.Core.Tests.Domain.Outbox;

public class OutboxMessageTests
{
    private class ConcreteEntity : Entity
    {
        public ConcreteEntity(Guid id) : base(id) { }
        public ConcreteEntity() : base() { }
    }

    [Fact]
    public void OutboxMessage_Properties_CanBeAssignedAndRead()
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var message = new OutboxMessage
        {
            Id = id,
            Type = "SeriesCreatedDomainEvent",
            Content = "{\"Title\":\"Test\"}",
            OccurredOnUtc = now,
            ProcessedOnUtc = now.AddSeconds(5),
            Error = "Temporary failure",
            RetryCount = 2
        };

        message.Id.Should().Be(id);
        message.Type.Should().Be("SeriesCreatedDomainEvent");
        message.Content.Should().Be("{\"Title\":\"Test\"}");
        message.OccurredOnUtc.Should().Be(now);
        message.ProcessedOnUtc.Should().Be(now.AddSeconds(5));
        message.Error.Should().Be("Temporary failure");
        message.RetryCount.Should().Be(2);
    }

    [Fact]
    public void Entity_Subclass_ShouldInheritFromBaseEntity()
    {
        var id = Guid.NewGuid();
        var entity = new ConcreteEntity(id);
        var defaultEntity = new ConcreteEntity();

        entity.Id.Should().Be(id);
        defaultEntity.Id.Should().Be(Guid.Empty);
    }
}
