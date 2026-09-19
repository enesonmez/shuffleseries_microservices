namespace ShuffleSeries.Shared.Core.Domain.Primitives;

public abstract class Entity : BaseEntity
{
    protected Entity(Guid id) : base(id)
    {
    }

    protected Entity()
    {
    }
}