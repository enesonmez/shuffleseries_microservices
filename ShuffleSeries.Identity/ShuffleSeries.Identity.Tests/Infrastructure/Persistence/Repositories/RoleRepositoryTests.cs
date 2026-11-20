using Microsoft.EntityFrameworkCore;
using ShuffleSeries.Identity.Domain.Entities;
using ShuffleSeries.Identity.Infrastructure.Persistence;
using ShuffleSeries.Identity.Infrastructure.Persistence.Repositories;

namespace ShuffleSeries.Identity.Tests.Infrastructure.Persistence.Repositories;

public class RoleRepositoryTests
{
    private static IdentityDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new IdentityDbContext(options);
    }

    [Fact]
    public async Task RepositoryOperations_ShouldHandleAddQueryUpdateAndDelete()
    {
        await using var context = CreateInMemoryContext();
        var sut = new RoleRepository(context);

        var permission = Permission.Create("catalog:movies:read", "Catalog", "Read movies");
        var defaultRole = Role.Create("standard", "Standard user", isDefault: true);
        var adminRole = Role.Create("admin", "Admin user", isDefault: false);
        adminRole.AddPermission(permission.Id);

        context.Permissions.Add(permission);
        sut.Add(defaultRole);
        sut.Add(adminRole);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var byId = await sut.GetByIdAsync(adminRole.Id);
        byId.Should().NotBeNull();
        byId!.RolePermissions.Should().ContainSingle();

        var byName = await sut.GetByNameAsync("  ADMIN ");
        byName.Should().NotBeNull();
        byName!.Id.Should().Be(adminRole.Id);

        var fetchedDefault = await sut.GetDefaultRoleAsync();
        fetchedDefault.Should().NotBeNull();
        fetchedDefault!.Id.Should().Be(defaultRole.Id);

        var rolesWithPerms = await sut.GetRolesWithPermissionsAsync([adminRole.Id]);
        rolesWithPerms.Should().ContainSingle()
            .Which.RolePermissions.Should().ContainSingle();

        var emptyRoles = await sut.GetRolesWithPermissionsAsync([]);
        emptyRoles.Should().BeEmpty();

        context.ChangeTracker.Clear();
        sut.Update(defaultRole);
        sut.Delete(defaultRole);
        await context.SaveChangesAsync();

        var deleted = await sut.GetByIdAsync(defaultRole.Id);
        deleted.Should().BeNull();
    }
}
