using ShuffleSeries.Shared.Core.Domain.Primitives;
using ShuffleSeries.Shared.Core.Infrastructure.Outbox;

namespace ShuffleSeries.Shared.Core.Tests.Infrastructure.Outbox;

public class DomainEventTypeCacheTests
{
    private sealed record SampleValidDomainEvent(Guid Id) : IDomainEvent;
    private abstract record SampleAbstractDomainEvent(Guid Id) : IDomainEvent;
    private sealed record SampleNotADomainEvent(Guid Id);

    [Fact]
    public void Resolve_WithValidDomainEventName_ShouldReturnType()
    {
        // Arrange
        var assembly = typeof(DomainEventTypeCacheTests).Assembly;

        // Act
        var result = DomainEventTypeCache.Resolve(assembly, nameof(SampleValidDomainEvent));

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(typeof(SampleValidDomainEvent));
    }

    [Fact]
    public void Resolve_WithValidDomainEventFullName_ShouldReturnType()
    {
        // Arrange
        var assembly = typeof(DomainEventTypeCacheTests).Assembly;

        // Act
        var result = DomainEventTypeCache.Resolve(assembly, typeof(SampleValidDomainEvent).FullName!);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(typeof(SampleValidDomainEvent));
    }

    [Fact]
    public void Resolve_WithNonExistentTypeName_ShouldReturnNull()
    {
        // Arrange
        var assembly = typeof(DomainEventTypeCacheTests).Assembly;

        // Act
        var result = DomainEventTypeCache.Resolve(assembly, "NonExistentDomainEvent");

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_WithNullOrWhitespaceTypeName_ShouldReturnNull(string? typeName)
    {
        // Arrange
        var assembly = typeof(DomainEventTypeCacheTests).Assembly;

        // Act
        var result = DomainEventTypeCache.Resolve(assembly, typeName!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Resolve_WithNullAssembly_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => DomainEventTypeCache.Resolve(null!, "SampleValidDomainEvent");

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Resolve_WithAbstractDomainEvent_ShouldReturnNull()
    {
        // Arrange
        var assembly = typeof(DomainEventTypeCacheTests).Assembly;

        // Act
        var result = DomainEventTypeCache.Resolve(assembly, nameof(SampleAbstractDomainEvent));

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Resolve_WithNonDomainEventType_ShouldReturnNull()
    {
        // Arrange
        var assembly = typeof(DomainEventTypeCacheTests).Assembly;

        // Act
        var result = DomainEventTypeCache.Resolve(assembly, nameof(SampleNotADomainEvent));

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Clear_ShouldClearCachedDictionaryWithoutThrowing()
    {
        // Arrange
        var assembly = typeof(DomainEventTypeCacheTests).Assembly;
        DomainEventTypeCache.Resolve(assembly, nameof(SampleValidDomainEvent));

        // Act
        DomainEventTypeCache.Clear();

        // Assert: Resolving again should repopulate cleanly
        var result = DomainEventTypeCache.Resolve(assembly, nameof(SampleValidDomainEvent));
        result.Should().Be(typeof(SampleValidDomainEvent));
    }
}
