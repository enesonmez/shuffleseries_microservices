using Moq;
using ShuffleSeries.Identity.Domain.Entities;
using ShuffleSeries.Identity.Domain.Repositories;
using ShuffleSeries.Identity.Infrastructure.Services;

namespace ShuffleSeries.Identity.Tests.Infrastructure;

public class PermissionResolverTests
{
    private readonly Mock<IRoleRepository> _roleRepositoryMock = new();
    private readonly Mock<IPermissionRepository> _permissionRepositoryMock = new();
    private readonly PermissionResolver _resolver;

    public PermissionResolverTests()
    {
        _resolver = new PermissionResolver(_roleRepositoryMock.Object, _permissionRepositoryMock.Object);
    }

    [Fact]
    public async Task ResolveEffectivePermissions_ShouldIncludeRolePermissions_AndCustomGrants_AndExcludeRevokes()
    {
        // Arrange: Roles and Permissions
        var permRead = Permission.Create(SystemPermissions.Catalog.MoviesRead, "Catalog", "Read movies");
        var permWrite = Permission.Create(SystemPermissions.Catalog.MoviesCreate, "Catalog", "Create movies");
        var permVip = Permission.Create(SystemPermissions.ShuffleVip, "Shuffle", "VIP arena");

        var standardRole = Role.Create("Standard", "Standard User");
        standardRole.AddPermission(permRead.Id);
        standardRole.AddPermission(permWrite.Id);

        // Inject navigation property for role permissions mock
        var rolePermissionsField = typeof(Role).GetField("_rolePermissions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var rps = (List<RolePermission>)rolePermissionsField.GetValue(standardRole)!;
        // Mock the navigation property Permission on RolePermission
        typeof(RolePermission).GetProperty(nameof(RolePermission.Permission))!.SetValue(rps[0], permRead);
        typeof(RolePermission).GetProperty(nameof(RolePermission.Permission))!.SetValue(rps[1], permWrite);

        var user = User.CreateStandard("user@test.com", "hash");
        user.AssignRole(standardRole.Id);

        // Custom override: Revoke "catalog:movies:create" and Grant "shuffle:vip"
        user.RevokePermission(permWrite.Id);
        user.GrantPermission(permVip.Id);

        _roleRepositoryMock.Setup(r => r.GetRolesWithPermissionsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([standardRole]);

        _permissionRepositoryMock.Setup(p => p.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([permWrite, permVip]);

        // Act
        var (roles, effectivePermissions) = await _resolver.ResolveEffectivePermissionsAsync(user);

        // Assert
        roles.Should().ContainSingle().Which.Should().Be("Standard");
        effectivePermissions.Should().Contain(SystemPermissions.Catalog.MoviesRead);
        effectivePermissions.Should().Contain(SystemPermissions.ShuffleVip);
        effectivePermissions.Should().NotContain(SystemPermissions.Catalog.MoviesCreate, "because catalog:movies:create was explicitly revoked for this user");
    }
}
