namespace ShuffleSeries.Shared.Core.Domain.Primitives;

public abstract class BaseEntity<TId> : IEquatable<BaseEntity<TId>>
{
    public TId Id { get; protected set; } = default!;
    public DateTime CreatedAtUtc { get; protected set; }
    public string? CreatedBy { get; protected set; }
    public DateTime? ModifiedAtUtc { get; protected set; }
    public string? ModifiedBy { get; protected set; }
    public DateTime? DeletedAtUtc { get; protected set; }
    public string? DeletedBy { get; protected set; }

    protected BaseEntity(TId id)
    {
        Id = id;
        CreatedAtUtc = DateTime.UtcNow;
    }

    protected BaseEntity()
    {
    }

    public bool Equals(BaseEntity<TId>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (other.GetType() != GetType()) return false;
        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((BaseEntity<TId>)obj);
    }

    public override int GetHashCode()
    {
        return EqualityComparer<TId>.Default.GetHashCode(Id!);
    }

    public static bool operator ==(BaseEntity<TId>? a, BaseEntity<TId>? b) =>
        a is null && b is null || a is not null && b is not null && a.Equals(b);

    public static bool operator !=(BaseEntity<TId>? a, BaseEntity<TId>? b) => !(a == b);
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