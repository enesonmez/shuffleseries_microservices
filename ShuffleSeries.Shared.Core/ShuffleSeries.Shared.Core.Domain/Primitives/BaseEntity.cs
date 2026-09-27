using System.ComponentModel.DataAnnotations.Schema;

namespace ShuffleSeries.Shared.Core.Domain.Primitives;

public abstract class BaseEntity<TId> : ISoftDeletable, IHardDeletable
{
    public TId Id { get; protected set; } = default!;
    public DateTime CreatedAtUtc { get; protected set; }
    public string? CreatedBy { get; protected set; }
    public DateTime? ModifiedAtUtc { get; protected set; }
    public string? ModifiedBy { get; protected set; }
    public bool IsDeleted { get; protected set; }
    public DateTime? DeletedAtUtc { get; protected set; }
    public string? DeletedBy { get; protected set; }

    [NotMapped]
    public bool IsHardDeleteRequested { get; protected set; }

    protected BaseEntity(TId id)
    {
        Id = id;
        CreatedAtUtc = DateTime.UtcNow;
        IsDeleted = false;
        IsHardDeleteRequested = false;
    }

    protected BaseEntity()
    {
    }

    public virtual void SoftDelete(string? deletedBy = null)
    {
        IsDeleted = true;
        DeletedAtUtc = DateTime.UtcNow;
        DeletedBy = deletedBy;
    }

    public virtual void UndoSoftDelete()
    {
        IsDeleted = false;
        DeletedAtUtc = null;
        DeletedBy = null;
        IsHardDeleteRequested = false;
    }

    public virtual void HardDelete() => IsHardDeleteRequested = true;

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        if (obj is not BaseEntity<TId> other) return false;
        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode() => EqualityComparer<TId>.Default.GetHashCode(Id!);
}

public abstract class BaseEntity : BaseEntity<Guid>
{
    protected BaseEntity(Guid id) : base(id)
    {
    }

    protected BaseEntity()
    {
    }
}
