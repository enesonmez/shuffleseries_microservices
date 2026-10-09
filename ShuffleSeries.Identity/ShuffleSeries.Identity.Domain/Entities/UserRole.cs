namespace ShuffleSeries.Identity.Domain.Entities;

public class UserRole
{
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

    public Guid RoleId { get; private set; }
    public Role Role { get; private set; } = null!;

    public DateTime AssignedAtUtc { get; private set; }

    private UserRole() { }

    public UserRole(Guid userId, Guid roleId, DateTime? assignedAtUtc = null)
    {
        UserId = userId;
        RoleId = roleId;
        AssignedAtUtc = assignedAtUtc ?? DateTime.UtcNow;
    }
}
