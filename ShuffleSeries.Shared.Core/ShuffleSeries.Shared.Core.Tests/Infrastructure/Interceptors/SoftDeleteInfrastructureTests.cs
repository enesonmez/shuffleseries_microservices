using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using ShuffleSeries.Shared.Core.Domain.Primitives;
using ShuffleSeries.Shared.Core.Infrastructure;
using ShuffleSeries.Shared.Core.Infrastructure.Extensions;
using ShuffleSeries.Shared.Core.Infrastructure.Interceptors;

namespace ShuffleSeries.Shared.Core.Tests.Infrastructure.Interceptors;

public class SoftDeleteInfrastructureTests
{
    private sealed class DummySoftDeletableEntity : BaseEntity
    {
        public string Name { get; set; } = string.Empty;

        public DummySoftDeletableEntity(Guid id, string name) : base(id)
        {
            Name = name;
        }

        private DummySoftDeletableEntity() { }
    }

    private sealed class DummyHardDeletableEntity
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    private sealed class DummyDbContext : DbContext
    {
        public DbSet<DummySoftDeletableEntity> SoftDeletables => Set<DummySoftDeletableEntity>();
        public DbSet<DummyHardDeletableEntity> HardDeletables => Set<DummyHardDeletableEntity>();

        public DummyDbContext(DbContextOptions<DummyDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplySoftDeleteQueryFilters();
        }
    }

    private static DummyDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<DummyDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(new SoftDeleteInterceptor())
            .Options;

        return new DummyDbContext(options);
    }

    [Fact]
    public async Task Remove_Should_ConvertToSoftDelete_AndPopulateAuditFields()
    {
        // Arrange
        await using var context = CreateDbContext();
        var entity = new DummySoftDeletableEntity(Guid.NewGuid(), "Breaking Bad");
        context.SoftDeletables.Add(entity);
        await context.SaveChangesAsync();

        // Act
        context.SoftDeletables.Remove(entity);
        await context.SaveChangesAsync();

        // Assert - verify in change tracker and database
        entity.IsDeleted.Should().BeTrue();
        entity.DeletedAtUtc.Should().NotBeNull();
        entity.DeletedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task Remove_Should_PreserveExistingDeletedBy_WhenDomainSoftDeleteAlreadyCalled()
    {
        // Arrange
        await using var context = CreateDbContext();
        var entity = new DummySoftDeletableEntity(Guid.NewGuid(), "Better Call Saul");
        context.SoftDeletables.Add(entity);
        await context.SaveChangesAsync();

        // Act - Domain soft delete called with specific user
        const string expectedUser = "security-auditor";
        entity.SoftDelete(expectedUser);

        context.SoftDeletables.Remove(entity);
        await context.SaveChangesAsync();

        // Assert - DeletedBy must not be overwritten with null
        var rawEntity = await context.SoftDeletables.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == entity.Id);
        rawEntity.Should().NotBeNull();
        rawEntity!.IsDeleted.Should().BeTrue();
        rawEntity.DeletedBy.Should().Be(expectedUser);
    }

    [Fact]
    public async Task Query_Should_AutomaticallyFilterOutSoftDeletedEntities()
    {
        // Arrange
        await using var context = CreateDbContext();
        var activeEntity = new DummySoftDeletableEntity(Guid.NewGuid(), "Active Show");
        var deletedEntity = new DummySoftDeletableEntity(Guid.NewGuid(), "Deleted Show");

        context.SoftDeletables.AddRange(activeEntity, deletedEntity);
        await context.SaveChangesAsync();

        // Soft delete the second entity
        context.SoftDeletables.Remove(deletedEntity);
        await context.SaveChangesAsync();

        // Act
        var activeList = await context.SoftDeletables.ToListAsync();

        // Assert
        activeList.Should().HaveCount(1);
        activeList[0].Id.Should().Be(activeEntity.Id);
    }

    [Fact]
    public async Task Query_WithIgnoreQueryFilters_ShouldReturnSoftDeletedEntities()
    {
        // Arrange
        await using var context = CreateDbContext();
        var activeEntity = new DummySoftDeletableEntity(Guid.NewGuid(), "Active Show");
        var deletedEntity = new DummySoftDeletableEntity(Guid.NewGuid(), "Deleted Show");

        context.SoftDeletables.AddRange(activeEntity, deletedEntity);
        await context.SaveChangesAsync();

        context.SoftDeletables.Remove(deletedEntity);
        await context.SaveChangesAsync();

        // Act
        var allList = await context.SoftDeletables.IgnoreQueryFilters().ToListAsync();

        // Assert
        allList.Should().HaveCount(2);
        allList.Should().Contain(x => x.Id == deletedEntity.Id && x.IsDeleted);
        allList.Should().Contain(x => x.Id == activeEntity.Id && !x.IsDeleted);
    }

    [Fact]
    public async Task HardDeletableEntity_ShouldBeDeletedPhysically()
    {
        // Arrange
        await using var context = CreateDbContext();
        var hardEntity = new DummyHardDeletableEntity { Id = Guid.NewGuid(), Title = "Temporary" };
        context.HardDeletables.Add(hardEntity);
        await context.SaveChangesAsync();

        // Act
        context.HardDeletables.Remove(hardEntity);
        await context.SaveChangesAsync();

        // Assert
        var result = await context.HardDeletables.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == hardEntity.Id);
        result.Should().BeNull();
    }

    [Fact]
    public async Task HardDeleteScope_Should_BypassSoftDelete_AndPhysicallyDeleteEntity()
    {
        // Arrange
        await using var context = CreateDbContext();
        var entity = new DummySoftDeletableEntity(Guid.NewGuid(), "To Be Hard Deleted");
        context.SoftDeletables.Add(entity);
        await context.SaveChangesAsync();

        // Act
        using (HardDeleteScope.Begin())
        {
            context.SoftDeletables.Remove(entity);
            await context.SaveChangesAsync();
        }

        // Assert - entity must not exist even with IgnoreQueryFilters
        var rawResult = await context.SoftDeletables.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == entity.Id);
        rawResult.Should().BeNull();
    }

    [Fact]
    public async Task EntityHardDelete_Should_BypassSoftDelete_ForSingleEntity()
    {
        // Arrange
        await using var context = CreateDbContext();
        var softEntity = new DummySoftDeletableEntity(Guid.NewGuid(), "Soft Deleted");
        var hardEntity = new DummySoftDeletableEntity(Guid.NewGuid(), "Hard Deleted");

        context.SoftDeletables.AddRange(softEntity, hardEntity);
        await context.SaveChangesAsync();

        // Act
        hardEntity.HardDelete(); // explicitly requested hard delete
        context.SoftDeletables.Remove(softEntity);
        context.SoftDeletables.Remove(hardEntity);
        await context.SaveChangesAsync();

        // Assert
        // softEntity should be soft deleted in DB
        var rawSoft = await context.SoftDeletables.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == softEntity.Id);
        rawSoft.Should().NotBeNull();
        rawSoft!.IsDeleted.Should().BeTrue();

        // hardEntity should be completely purged
        var rawHard = await context.SoftDeletables.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == hardEntity.Id);
        rawHard.Should().BeNull();
    }

    [Fact]
    public void HardDeleteScope_NestedScopes_ShouldPreserveOuterState()
    {
        HardDeleteScope.IsActive.Should().BeFalse();

        using (HardDeleteScope.Begin())
        {
            HardDeleteScope.IsActive.Should().BeTrue();

            using (HardDeleteScope.Begin())
            {
                HardDeleteScope.IsActive.Should().BeTrue();
            }

            // Outer scope must still be active after inner scope disposes
            HardDeleteScope.IsActive.Should().BeTrue();
        }

        HardDeleteScope.IsActive.Should().BeFalse();
    }
}
