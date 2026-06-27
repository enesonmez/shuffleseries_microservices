namespace ShuffleSeries.Shared.Core.Domain.Primitives;

public abstract class Entity : IEquatable<Entity>
{
    public Guid Id { get; protected set; }
    public DateTime CreatedAtUtc { get; protected set; }
    public string? CreatedBy { get; protected set; }
    public DateTime ModifiedAtUtc { get; protected set; }
    public string? ModifiedBy { get; protected set; }
    public DateTime DeletedAtUtc { get; protected set; }
    public string? DeletedBy { get; protected set; }
    
    protected Entity(Guid id)
    {
        Id = id;
        CreatedAtUtc = DateTime.UtcNow;
    }
    
    protected Entity() {}
    
    public bool Equals(Entity? other)
    {
        return other is not null && other.GetType() == GetType() && other.Id == Id;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((Entity)obj);
    }

    public override int GetHashCode() => Id.GetHashCode();
    
    public static bool operator ==(Entity? a, Entity? b) =>
        a is null && b is null || a is not null && b is not null && a.Equals(b);

    public static bool operator !=(Entity? a, Entity? b) => !(a == b);
}