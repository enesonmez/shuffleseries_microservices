using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.Identity.Domain.Entities;

public class Permission : AggregateRoot
{
    public string Code { get; private set; } = string.Empty;
    public string Group { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    private Permission() { }

    private Permission(Guid id, string code, string group, string description) : base(id)
    {
        Code = code;
        Group = group;
        Description = description;
    }

    public static Permission Create(string code, string group, string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(group);

        return new Permission(Guid.NewGuid(), code.Trim().ToLowerInvariant(), group.Trim(), description.Trim());
    }
}
