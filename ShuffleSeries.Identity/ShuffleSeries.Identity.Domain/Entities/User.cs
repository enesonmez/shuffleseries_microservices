using ShuffleSeries.Identity.Domain.Enums;
using ShuffleSeries.Identity.Domain.Events;
using ShuffleSeries.Identity.Domain.Exceptions;
using ShuffleSeries.Shared.Core.Domain.Primitives;

namespace ShuffleSeries.Identity.Domain.Entities;

public class User : AggregateRoot
{
    private readonly List<UserRole> _userRoles = [];
    private readonly List<UserPermission> _userPermissions = [];
    private readonly List<UserLogin> _userLogins = [];
    private readonly List<RefreshToken> _refreshTokens = [];

    public string Email { get; private set; } = string.Empty;
    public string NormalizedEmail { get; private set; } = string.Empty;
    public string? PasswordHash { get; private set; }
    public string SecurityStamp { get; private set; } = string.Empty;
    public UserStatus Status { get; private set; }
    public bool IsGuest { get; private set; }

    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();
    public IReadOnlyCollection<UserPermission> UserPermissions => _userPermissions.AsReadOnly();
    public IReadOnlyCollection<UserLogin> UserLogins => _userLogins.AsReadOnly();
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    private User() { }

    private User(Guid id, string email, string? passwordHash, bool isGuest, UserStatus status) : base(id)
    {
        Email = email;
        NormalizedEmail = email.ToUpperInvariant();
        PasswordHash = passwordHash;
        IsGuest = isGuest;
        Status = status;
        SecurityStamp = Guid.NewGuid().ToString("N");
    }

    public static User CreateStandard(string email, string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        var trimmedEmail = email.Trim().ToLowerInvariant();
        var user = new User(Guid.NewGuid(), trimmedEmail, passwordHash, isGuest: false, UserStatus.Active);
        user.RaiseDomainEvent(new UserRegisteredDomainEvent(user.Id, user.Email, IsGuest: false));

        return user;
    }

    public static User CreateGuest()
    {
        var guestId = Guid.NewGuid();
        var guestEmail = $"guest_{guestId:N}@shuffleseries.internal";
        var user = new User(guestId, guestEmail, passwordHash: null, isGuest: true, UserStatus.Guest);
        user.RaiseDomainEvent(new UserRegisteredDomainEvent(user.Id, user.Email, IsGuest: true));

        return user;
    }

    public static User CreateSocial(string email, string provider, string providerKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerKey);

        var trimmedEmail = email.Trim().ToLowerInvariant();
        var user = new User(Guid.NewGuid(), trimmedEmail, passwordHash: null, isGuest: false, UserStatus.Active);
        user.AddLogin(provider, providerKey, trimmedEmail);
        user.RaiseDomainEvent(new UserRegisteredDomainEvent(user.Id, user.Email, IsGuest: false));

        return user;
    }

    public void AssignRole(Guid roleId, DateTime? assignedAtUtc = null)
    {
        if (_userRoles.All(ur => ur.RoleId != roleId))
        {
            _userRoles.Add(new UserRole(Id, roleId, assignedAtUtc));
            TouchSecurityStamp();
        }
    }

    public void RemoveRole(Guid roleId)
    {
        var existing = _userRoles.FirstOrDefault(ur => ur.RoleId == roleId);
        if (existing is not null)
        {
            _userRoles.Remove(existing);
            TouchSecurityStamp();
        }
    }

    public void GrantPermission(Guid permissionId, DateTime? assignedAtUtc = null)
    {
        var existing = _userPermissions.FirstOrDefault(up => up.PermissionId == permissionId);
        if (existing is not null)
        {
            existing.UpdateGrant(true, assignedAtUtc);
        }
        else
        {
            _userPermissions.Add(new UserPermission(Id, permissionId, isGranted: true, assignedAtUtc));
        }

        TouchSecurityStamp();
    }

    public void RevokePermission(Guid permissionId, DateTime? assignedAtUtc = null)
    {
        var existing = _userPermissions.FirstOrDefault(up => up.PermissionId == permissionId);
        if (existing is not null)
        {
            existing.UpdateGrant(false, assignedAtUtc);
        }
        else
        {
            _userPermissions.Add(new UserPermission(Id, permissionId, isGranted: false, assignedAtUtc));
        }

        TouchSecurityStamp();
    }

    public void RemovePermissionOverride(Guid permissionId)
    {
        var existing = _userPermissions.FirstOrDefault(up => up.PermissionId == permissionId);
        if (existing is not null)
        {
            _userPermissions.Remove(existing);
            TouchSecurityStamp();
        }
    }

    public void AddLogin(string provider, string providerKey, string? providerEmail = null, string? refreshToken = null, DateTime? linkedAtUtc = null)
    {
        if (_userLogins.All(ul => ul.Provider != provider || ul.ProviderKey != providerKey))
        {
            _userLogins.Add(new UserLogin(Id, provider, providerKey, providerEmail, refreshToken, linkedAtUtc));
        }
    }

    public RefreshToken AddRefreshToken(string tokenHash, DateTime expiresAtUtc, string? createdByIp = null)
    {
        var refreshToken = new RefreshToken(Id, tokenHash, expiresAtUtc, createdByIp);
        _refreshTokens.Add(refreshToken);
        return refreshToken;
    }

    public void RevokeRefreshToken(string tokenHash, string? replacedByTokenHash = null, DateTime? revokedAtUtc = null)
    {
        var token = _refreshTokens.FirstOrDefault(rt => rt.TokenHash == tokenHash);
        token?.Revoke(replacedByTokenHash, revokedAtUtc);
    }

    public void RevokeAllRefreshTokens(DateTime? revokedAtUtc = null)
    {
        foreach (var token in _refreshTokens.Where(rt => rt.IsActive))
        {
            token.Revoke(revokedAtUtc: revokedAtUtc);
        }

        TouchSecurityStamp();
    }

    public void UpdatePassword(string newPasswordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newPasswordHash);
        PasswordHash = newPasswordHash;
        RevokeAllRefreshTokens();
        TouchSecurityStamp();
    }

    public void ConvertFromGuest(string email, string passwordHash)
    {
        if (!IsGuest)
        {
            throw new UserAlreadyRegisteredException();
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        Email = email.Trim().ToLowerInvariant();
        NormalizedEmail = Email.ToUpperInvariant();
        PasswordHash = passwordHash;
        IsGuest = false;
        Status = UserStatus.Active;

        RevokeAllRefreshTokens();
        TouchSecurityStamp();

        RaiseDomainEvent(new UserRegisteredDomainEvent(Id, Email, IsGuest: false));
    }

    public void DeleteAccount()
    {
        Status = UserStatus.Deleted;
        SoftDelete();
        RevokeAllRefreshTokens();
        TouchSecurityStamp();

        RaiseDomainEvent(new UserAccountDeletedDomainEvent(Id, Email));
    }

    public void TouchSecurityStamp() => SecurityStamp = Guid.NewGuid().ToString("N");
}
