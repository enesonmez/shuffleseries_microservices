using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.Identity.Domain.Entities;

public class Role : AggregateRoot
{
    private readonly List<RolePermission> _rolePermissions = [];

    public string Name { get; private set; } = string.Empty;
    public string NormalizedName { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public bool IsDefault { get; private set; }

    public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions.AsReadOnly();

    private Role() { }

    private Role(Guid id, string name, string description, bool isDefault) : base(id)
    {
        Name = name;
        NormalizedName = name.ToUpperInvariant();
        Description = description;
        IsDefault = isDefault;
    }

    public static Role Create(string name, string description, bool isDefault = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Role(Guid.NewGuid(), name.Trim(), description.Trim(), isDefault);
    }

    public void AddPermission(Guid permissionId)
    {
        if (_rolePermissions.All(rp => rp.PermissionId != permissionId))
        {
            _rolePermissions.Add(new RolePermission(Id, permissionId));
        }
    }

    public void RemovePermission(Guid permissionId)
    {
        var existing = _rolePermissions.FirstOrDefault(rp => rp.PermissionId == permissionId);
        if (existing is not null)
        {
            _rolePermissions.Remove(existing);
        }
    }
}
