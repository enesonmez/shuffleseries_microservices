using FluentAssertions;
using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.Shared.Core.Tests.Domain.Primitives;

public class BaseEntityTests
{
    private sealed class TestEntity : BaseEntity
    {
        public TestEntity(Guid id) : base(id) { }
        public TestEntity() { }

        public void SetAudit(DateTime? modifiedAt, string? modifiedBy, DateTime? deletedAt, string? deletedBy)
        {
            ModifiedAtUtc = modifiedAt;
            ModifiedBy = modifiedBy;
            DeletedAtUtc = deletedAt;
            DeletedBy = deletedBy;
        }
    }

    private sealed class AnotherTestEntity : BaseEntity
    {
        public AnotherTestEntity(Guid id) : base(id) { }
    }

    private sealed class GenericTestEntity : BaseEntity<string>
    {
        public GenericTestEntity(string id) : base(id) { }
    }

    [Fact]
    public void Constructor_WithId_ShouldSetIdAndCreatedAtUtc()
    {
        var id = Guid.NewGuid();
        var entity = new TestEntity(id);

        entity.Id.Should().Be(id);
        entity.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        entity.ModifiedAtUtc.Should().BeNull();
        entity.DeletedAtUtc.Should().BeNull();
        entity.CreatedBy.Should().BeNull();
        entity.ModifiedBy.Should().BeNull();
        entity.DeletedBy.Should().BeNull();
    }

    [Fact]
    public void Equals_WithSameIdAndType_ShouldReturnTrue()
    {
        var id = Guid.NewGuid();
        var entity1 = new TestEntity(id);
        var entity2 = new TestEntity(id);

        entity1.Equals(entity2).Should().BeTrue();
        (entity1 == entity2).Should().BeTrue();
        (entity1 != entity2).Should().BeFalse();
        entity1.GetHashCode().Should().Be(entity2.GetHashCode());
    }

    [Fact]
    public void Equals_WithDifferentId_ShouldReturnFalse()
    {
        var entity1 = new TestEntity(Guid.NewGuid());
        var entity2 = new TestEntity(Guid.NewGuid());

        entity1.Equals(entity2).Should().BeFalse();
        (entity1 == entity2).Should().BeFalse();
        (entity1 != entity2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentType_ShouldReturnFalse()
    {
        var id = Guid.NewGuid();
        var entity1 = new TestEntity(id);
        var entity2 = new AnotherTestEntity(id);

        entity1.Equals(entity2).Should().BeFalse();
        (entity1 == entity2).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        var entity = new TestEntity(Guid.NewGuid());

        entity.Equals(null).Should().BeFalse();
        (entity == null).Should().BeFalse();
        (null == entity).Should().BeFalse();
        (entity != null).Should().BeTrue();
        ((TestEntity?)null == (TestEntity?)null).Should().BeTrue();
        ((TestEntity?)null != (TestEntity?)null).Should().BeFalse();
    }

    [Fact]
    public void GenericBaseEntity_WithStringId_ShouldWorkCorrectly()
    {
        var entity1 = new GenericTestEntity("item-1");
        var entity2 = new GenericTestEntity("item-1");
        var entity3 = new GenericTestEntity("item-2");

        entity1.Id.Should().Be("item-1");
        (entity1 == entity2).Should().BeTrue();
        (entity1 == entity3).Should().BeFalse();
    }

    [Fact]
    public void AuditProperties_CanBeAssignedAndRead()
    {
        var entity = new TestEntity(Guid.NewGuid());
        var now = DateTime.UtcNow;

        entity.SetAudit(now, "admin", now.AddMinutes(5), "system");

        entity.ModifiedAtUtc.Should().Be(now);
        entity.ModifiedBy.Should().Be("admin");
        entity.DeletedAtUtc.Should().Be(now.AddMinutes(5));
        entity.DeletedBy.Should().Be("system");
    }

    [Fact]
    public void IsDeleted_ShouldBeFalse_ByDefault()
    {
        var entity = new TestEntity(Guid.NewGuid());

        entity.IsDeleted.Should().BeFalse();
        entity.DeletedAtUtc.Should().BeNull();
        entity.DeletedBy.Should().BeNull();
    }

    [Fact]
    public void SoftDelete_ShouldSetIsDeletedTrue_AndPopulateAuditFields()
    {
        var entity = new TestEntity(Guid.NewGuid());
        const string deletedBy = "admin-user";

        entity.SoftDelete(deletedBy);

        entity.IsDeleted.Should().BeTrue();
        entity.DeletedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        entity.DeletedBy.Should().Be(deletedBy);
    }

    [Fact]
    public void UndoSoftDelete_ShouldResetIsDeleted_AndClearAuditFields()
    {
        var entity = new TestEntity(Guid.NewGuid());
        entity.SoftDelete("admin-user");

        entity.UndoSoftDelete();

        entity.IsDeleted.Should().BeFalse();
        entity.DeletedAtUtc.Should().BeNull();
        entity.DeletedBy.Should().BeNull();
        entity.IsHardDeleteRequested.Should().BeFalse();
    }

    [Fact]
    public void HardDelete_ShouldSetIsHardDeleteRequestedTrue()
    {
        var entity = new TestEntity(Guid.NewGuid());
        entity.IsHardDeleteRequested.Should().BeFalse();

        entity.HardDelete();

        entity.IsHardDeleteRequested.Should().BeTrue();
    }
}
