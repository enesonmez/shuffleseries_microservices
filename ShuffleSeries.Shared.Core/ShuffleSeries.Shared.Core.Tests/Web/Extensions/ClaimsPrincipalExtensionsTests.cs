using System.Security.Claims;
using ShuffleSeries.Shared.Core.Exceptions;
using ShuffleSeries.Shared.Core.Web.Extensions;

namespace ShuffleSeries.Shared.Core.Tests.Web.Extensions;

public class ClaimsPrincipalExtensionsTests
{
    [Fact]
    public void GetUserId_WithSubClaim_ShouldReturnGuid()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var claims = new[] { new Claim("sub", expectedId.ToString()) };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));

        // Act
        var result = principal.GetUserId();

        // Assert
        result.Should().Be(expectedId);
    }

    [Fact]
    public void GetUserId_WithNameIdentifierClaim_ShouldReturnGuid()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, expectedId.ToString()) };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));

        // Act
        var result = principal.GetUserId();

        // Assert
        result.Should().Be(expectedId);
    }

    [Fact]
    public void GetUserId_WithoutClaim_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        var act = () => principal.GetUserId();

        // Assert
        act.Should().Throw<UnauthorizedException>()
            .WithMessage("*User ID could not be identified*");
    }

    [Fact]
    public void GetUserId_WithInvalidGuid_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var claims = new[] { new Claim("sub", "not-a-guid") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        // Act
        var act = () => principal.GetUserId();

        // Assert
        act.Should().Throw<UnauthorizedException>();
    }

    [Fact]
    public void TryGetUserId_WithValidGuid_ShouldReturnGuid()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var claims = new[] { new Claim("sub", expectedId.ToString()) };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        // Act
        var result = principal.TryGetUserId();

        // Assert
        result.Should().Be(expectedId);
    }

    [Fact]
    public void TryGetUserId_WithoutClaim_ShouldReturnNull()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        var result = principal.TryGetUserId();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetEmail_WithEmailClaim_ShouldReturnEmail()
    {
        // Arrange
        const string expectedEmail = "user@shuffleseries.com";
        var claims = new[] { new Claim(ClaimTypes.Email, expectedEmail) };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        // Act
        var result = principal.GetEmail();

        // Assert
        result.Should().Be(expectedEmail);
    }

    [Fact]
    public void IsGuest_WithGuestClaimTrue_ShouldReturnTrue()
    {
        // Arrange
        var claims = new[] { new Claim("is_guest", "true") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        // Act
        var result = principal.IsGuest();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsGuest_WithoutGuestClaim_ShouldReturnFalse()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        var result = principal.IsGuest();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetRoles_ShouldReturnDistinctRoles()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.Role, "Standard"),
            new Claim("role", "Standard"),
            new Claim(ClaimTypes.Role, "Admin")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        // Act
        var roles = principal.GetRoles().ToList();

        // Assert
        roles.Should().HaveCount(2);
        roles.Should().Contain(["Standard", "Admin"]);
    }

    [Fact]
    public void GetPermissions_ShouldReturnDistinctPermissions()
    {
        // Arrange
        var claims = new[]
        {
            new Claim("permissions", "catalog:read"),
            new Claim("permissions", "catalog:read"),
            new Claim("permissions", "catalog:create")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        // Act
        var perms = principal.GetPermissions().ToList();

        // Assert
        perms.Should().HaveCount(2);
        perms.Should().Contain(["catalog:read", "catalog:create"]);
    }

    [Fact]
    public void HasPermission_ShouldReturnExpectedResult()
    {
        // Arrange
        var claims = new[] { new Claim("permissions", "catalog:read") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        // Act & Assert
        principal.HasPermission("catalog:read").Should().BeTrue();
        principal.HasPermission("catalog:delete").Should().BeFalse();
    }
}
