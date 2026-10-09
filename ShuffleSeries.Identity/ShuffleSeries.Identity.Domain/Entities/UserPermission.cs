namespace ShuffleSeries.Identity.Domain.Entities;

public class UserPermission
{
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

    public Guid PermissionId { get; private set; }
    public Permission Permission { get; private set; } = null!;

    public bool IsGranted { get; private set; }
    public DateTime AssignedAtUtc { get; private set; }

    private UserPermission() { }

    public UserPermission(Guid userId, Guid permissionId, bool isGranted, DateTime? assignedAtUtc = null)
    {
        UserId = userId;
        PermissionId = permissionId;
        IsGranted = isGranted;
        AssignedAtUtc = assignedAtUtc ?? DateTime.UtcNow;
    }

    public void UpdateGrant(bool isGranted, DateTime? assignedAtUtc = null)
    {
        IsGranted = isGranted;
        AssignedAtUtc = assignedAtUtc ?? DateTime.UtcNow;
    }
}
