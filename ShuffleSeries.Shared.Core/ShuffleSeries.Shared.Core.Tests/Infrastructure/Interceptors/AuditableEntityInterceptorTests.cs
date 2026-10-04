using Microsoft.EntityFrameworkCore;
using ShuffleSeries.Shared.Core.Domain.Primitives;
using ShuffleSeries.Shared.Core.Infrastructure.Interceptors;

namespace ShuffleSeries.Shared.Core.Tests.Infrastructure.Interceptors;

public class AuditableEntityInterceptorTests
{
    private sealed class TestTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset _utcNow = utcNow;
        public override DateTimeOffset GetUtcNow() => _utcNow;
        public void Advance(TimeSpan timeSpan) => _utcNow = _utcNow.Add(timeSpan);
    }

    private sealed class DummyAuditableEntity : BaseEntity
    {
        public string Title { get; set; } = string.Empty;

        public DummyAuditableEntity(Guid id, string title) : base(id)
        {
            Title = title;
        }

        private DummyAuditableEntity() { }
    }

    private sealed class DummyNonAuditableEntity
    {
        public Guid Id { get; set; }
        public string Data { get; set; } = string.Empty;
    }

    private sealed class AuditableTestDbContext : DbContext
    {
        public DbSet<DummyAuditableEntity> Auditables => Set<DummyAuditableEntity>();
        public DbSet<DummyNonAuditableEntity> NonAuditables => Set<DummyNonAuditableEntity>();

        public AuditableTestDbContext(DbContextOptions<AuditableTestDbContext> options) : base(options) { }
    }

    private static AuditableTestDbContext CreateDbContext(TimeProvider timeProvider)
    {
        var options = new DbContextOptionsBuilder<AuditableTestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(new AuditableEntityInterceptor(timeProvider))
            .Options;

        return new AuditableTestDbContext(options);
    }

    [Fact]
    public async Task SavingChangesAsync_WhenEntityAdded_ShouldSetCreatedAtUtcUsingTimeProvider()
    {
        // Arrange
        var expectedTime = new DateTimeOffset(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);
        var timeProvider = new TestTimeProvider(expectedTime);
        await using var context = CreateDbContext(timeProvider);

        var entity = new DummyAuditableEntity(Guid.NewGuid(), "Initial Title");

        // Act
        context.Auditables.Add(entity);
        await context.SaveChangesAsync();

        // Assert
        entity.CreatedAtUtc.Should().Be(expectedTime.UtcDateTime);
        entity.ModifiedAtUtc.Should().BeNull();
    }

    [Fact]
    public void SavingChanges_Sync_WhenEntityAdded_ShouldSetCreatedAtUtcUsingTimeProvider()
    {
        // Arrange
        var expectedTime = new DateTimeOffset(2026, 10, 4, 14, 30, 0, TimeSpan.Zero);
        var timeProvider = new TestTimeProvider(expectedTime);
        using var context = CreateDbContext(timeProvider);

        var entity = new DummyAuditableEntity(Guid.NewGuid(), "Sync Entity");

        // Act
        context.Auditables.Add(entity);
        context.SaveChanges();

        // Assert
        entity.CreatedAtUtc.Should().Be(expectedTime.UtcDateTime);
        entity.ModifiedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task SavingChangesAsync_WhenEntityModified_ShouldSetModifiedAtUtcUsingTimeProvider()
    {
        // Arrange
        var initialTime = new DateTimeOffset(2026, 10, 4, 10, 0, 0, TimeSpan.Zero);
        var timeProvider = new TestTimeProvider(initialTime);
        await using var context = CreateDbContext(timeProvider);

        var entity = new DummyAuditableEntity(Guid.NewGuid(), "Original");
        context.Auditables.Add(entity);
        await context.SaveChangesAsync();

        // Act - advance time by 2 hours and update property
        timeProvider.Advance(TimeSpan.FromHours(2));
        var expectedModifiedTime = initialTime.AddHours(2);

        entity.Title = "Updated Title";
        await context.SaveChangesAsync();

        // Assert
        entity.CreatedAtUtc.Should().Be(initialTime.UtcDateTime);
        entity.ModifiedAtUtc.Should().NotBeNull();
        entity.ModifiedAtUtc.Should().Be(expectedModifiedTime.UtcDateTime);
    }

    [Fact]
    public async Task SavingChangesAsync_WhenMultipleEntitiesAdded_ShouldSetCreatedAtUtcForAll()
    {
        // Arrange
        var fixedTime = new DateTimeOffset(2026, 10, 4, 15, 0, 0, TimeSpan.Zero);
        var timeProvider = new TestTimeProvider(fixedTime);
        await using var context = CreateDbContext(timeProvider);

        var entity1 = new DummyAuditableEntity(Guid.NewGuid(), "First");
        var entity2 = new DummyAuditableEntity(Guid.NewGuid(), "Second");

        // Act
        context.Auditables.AddRange(entity1, entity2);
        await context.SaveChangesAsync();

        // Assert
        entity1.CreatedAtUtc.Should().Be(fixedTime.UtcDateTime);
        entity2.CreatedAtUtc.Should().Be(fixedTime.UtcDateTime);
        entity1.ModifiedAtUtc.Should().BeNull();
        entity2.ModifiedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task SavingChangesAsync_NonAuditableEntity_ShouldNotThrowAndSaveNormally()
    {
        // Arrange
        var timeProvider = new TestTimeProvider(DateTimeOffset.UtcNow);
        await using var context = CreateDbContext(timeProvider);

        var nonAuditable = new DummyNonAuditableEntity { Id = Guid.NewGuid(), Data = "No Audit" };

        // Act
        context.NonAuditables.Add(nonAuditable);
        var savedCount = await context.SaveChangesAsync();

        // Assert
        savedCount.Should().Be(1);
    }
}
