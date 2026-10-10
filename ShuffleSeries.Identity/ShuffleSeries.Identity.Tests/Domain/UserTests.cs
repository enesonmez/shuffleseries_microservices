using ShuffleSeries.Identity.Domain.Entities;
using ShuffleSeries.Identity.Domain.Enums;
using ShuffleSeries.Identity.Domain.Events;

namespace ShuffleSeries.Identity.Tests.Domain;

public class UserTests
{
    [Fact]
    public void CreateStandard_ShouldInitializeCorrectly_AndRaiseRegisteredDomainEvent()
    {
        // Act
        var user = User.CreateStandard("Test@ShuffleSeries.com", "HashedPassword123");

        // Assert
        user.Email.Should().Be("test@shuffleseries.com");
        user.NormalizedEmail.Should().Be("TEST@SHUFFLESERIES.COM");
        user.PasswordHash.Should().Be("HashedPassword123");
        user.IsGuest.Should().BeFalse();
        user.Status.Should().Be(UserStatus.Active);
        user.SecurityStamp.Should().NotBeNullOrWhiteSpace();

        var events = user.GetDomainEvents();
        events.Should().ContainSingle()
            .Which.Should().BeOfType<UserRegisteredDomainEvent>()
            .Which.IsGuest.Should().BeFalse();
    }

    [Fact]
    public void CreateGuest_ShouldCreateGuestUser_WithGuestRoleFlag()
    {
        // Act
        var user = User.CreateGuest();

        // Assert
        user.IsGuest.Should().BeTrue();
        user.Status.Should().Be(UserStatus.Guest);
        user.PasswordHash.Should().BeNull();
        user.Email.Should().StartWith("guest_");

        var events = user.GetDomainEvents();
        events.Should().ContainSingle()
            .Which.Should().BeOfType<UserRegisteredDomainEvent>()
            .Which.IsGuest.Should().BeTrue();
    }

    [Fact]
    public void AssignRole_ShouldAddRole_AndTouchSecurityStamp()
    {
        // Arrange
        var user = User.CreateStandard("user@test.com", "hash");
        var initialStamp = user.SecurityStamp;
        var roleId = Guid.NewGuid();

        // Act
        user.AssignRole(roleId);

        // Assert
        user.UserRoles.Should().ContainSingle(ur => ur.RoleId == roleId);
        user.SecurityStamp.Should().NotBe(initialStamp);
    }

    [Fact]
    public void GrantAndRevokePermission_ShouldManageCustomOverrides()
    {
        // Arrange
        var user = User.CreateStandard("user@test.com", "hash");
        var permId = Guid.NewGuid();

        // Act: Grant
        user.GrantPermission(permId);

        // Assert
        user.UserPermissions.Should().ContainSingle(up => up.PermissionId == permId && up.IsGranted);

        // Act: Revoke
        user.RevokePermission(permId);

        // Assert
        user.UserPermissions.Should().ContainSingle(up => up.PermissionId == permId && !up.IsGranted);

        // Act: Remove override
        user.RemovePermissionOverride(permId);
        user.UserPermissions.Should().BeEmpty();
    }

    [Fact]
    public void AddRefreshToken_ShouldStoreToken_AndRevokeShouldInvalidateIt()
    {
        // Arrange
        var user = User.CreateStandard("user@test.com", "hash");
        var tokenHash = "sha256_dummy_hash";

        // Act: Add
        var token = user.AddRefreshToken(tokenHash, DateTime.UtcNow.AddDays(7), "127.0.0.1");

        // Assert
        token.IsActive.Should().BeTrue();
        user.RefreshTokens.Should().ContainSingle();

        // Act: Revoke
        user.RevokeRefreshToken(tokenHash, "replacement_hash");

        // Assert
        token.IsRevoked.Should().BeTrue();
        token.IsActive.Should().BeFalse();
        token.ReplacedByTokenHash.Should().Be("replacement_hash");
    }

    [Fact]
    public void DeleteAccount_ShouldSoftDelete_AndRaiseUserAccountDeletedEvent()
    {
        // Arrange
        var user = User.CreateStandard("user@test.com", "hash");
        user.AddRefreshToken("hash1", DateTime.UtcNow.AddDays(7));

        // Act
        user.DeleteAccount();

        // Assert
        user.Status.Should().Be(UserStatus.Deleted);
        user.IsDeleted.Should().BeTrue();
        user.RefreshTokens.All(t => t.IsRevoked).Should().BeTrue();

        user.GetDomainEvents().Should().Contain(e => e is UserAccountDeletedDomainEvent);
    }

    [Fact]
    public void ConvertFromGuest_WhenUserIsNotGuest_ShouldThrowUserAlreadyRegisteredException()
    {
        // Arrange
        var user = User.CreateStandard("user@test.com", "hash");

        // Act
        var act = () => user.ConvertFromGuest("new@test.com", "newhash");

        // Assert
        var ex = act.Should().Throw<ShuffleSeries.Identity.Domain.Exceptions.UserAlreadyRegisteredException>();
        ex.Which.Code.Should().Be("USER_ALREADY_REGISTERED");
    }

    [Fact]
    public void ConvertFromGuest_WhenUserIsGuest_ShouldUpdateProperties_AndClearGuestFlag()
    {
        // Arrange
        var user = User.CreateGuest();

        // Act
        user.ConvertFromGuest("converted@test.com", "newhash");

        // Assert
        user.IsGuest.Should().BeFalse();
        user.Email.Should().Be("converted@test.com");
        user.NormalizedEmail.Should().Be("CONVERTED@TEST.COM");
        user.PasswordHash.Should().Be("newhash");
        user.Status.Should().Be(UserStatus.Active);
    }

    [Fact]
    public void CreateSocial_ShouldAddUserLogin_AndRaiseUserRegisteredDomainEvent()
    {
        // Act
        var user = User.CreateSocial("social@test.com", "Google", "google-sub-12345");

        // Assert
        user.Email.Should().Be("social@test.com");
        user.IsGuest.Should().BeFalse();
        user.Status.Should().Be(UserStatus.Active);
        user.UserLogins.Should().ContainSingle(l => l.Provider == "Google" && l.ProviderKey == "google-sub-12345");

        var domainEvents = user.GetDomainEvents();
        domainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<UserRegisteredDomainEvent>()
            .Which.IsGuest.Should().BeFalse();
    }

    [Fact]
    public void MergeGuest_ShouldRaiseUserMergedDomainEvent()
    {
        // Arrange
        var targetUser = User.CreateStandard("target@test.com", "hash");
        targetUser.ClearDomainEvents();
        var guestUserId = Guid.NewGuid();

        // Act
        targetUser.MergeGuest(guestUserId);

        // Assert
        var domainEvents = targetUser.GetDomainEvents();
        domainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<UserMergedDomainEvent>()
            .Which.Should().Match<UserMergedDomainEvent>(e => e.TargetUserId == targetUser.Id && e.GuestUserId == guestUserId);
    }
}
