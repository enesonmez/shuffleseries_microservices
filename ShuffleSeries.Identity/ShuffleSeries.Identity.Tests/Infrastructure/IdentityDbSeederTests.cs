using Microsoft.EntityFrameworkCore;
using ShuffleSeries.Identity.Infrastructure.Persistence;

namespace ShuffleSeries.Identity.Tests.Infrastructure;

public class IdentityDbSeederTests
{
    private static IdentityDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new IdentityDbContext(options);
    }

    [Fact]
    public async Task SeedAsync_ShouldSeedAllGranularCatalogPermissions_AndAssignToRoles()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var context = CreateInMemoryContext(dbName);

        // Act
        await IdentityDbSeeder.SeedAsync(context);

        // Assert Permissions
        var permissions = await context.Permissions.ToListAsync();
        permissions.Should().NotBeEmpty();

        // Verify granular catalog permissions exist
        string[] expectedPermissions =
        [
            SystemPermissions.Catalog.MoviesRead,
            SystemPermissions.Catalog.MoviesCreate,
            SystemPermissions.Catalog.MoviesUpdate,
            SystemPermissions.Catalog.MoviesDelete,
            SystemPermissions.Catalog.SeriesRead,
            SystemPermissions.Catalog.SeriesCreate,
            SystemPermissions.Catalog.SeriesUpdate,
            SystemPermissions.Catalog.SeriesDelete,
            SystemPermissions.Catalog.EpisodesRead,
            SystemPermissions.Catalog.EpisodesCreate,
            SystemPermissions.Catalog.EpisodesUpdate,
            SystemPermissions.Catalog.EpisodesDelete,
            SystemPermissions.Catalog.PlatformsRead,
            SystemPermissions.Catalog.PlatformsCreate,
            SystemPermissions.Catalog.PlatformsUpdate,
            SystemPermissions.Catalog.PlatformsDelete,
            SystemPermissions.Catalog.MoodsRead,
            SystemPermissions.Catalog.MoodsCreate,
            SystemPermissions.Catalog.MoodsUpdate,
            SystemPermissions.Catalog.MoodsDelete,
            SystemPermissions.Catalog.GenresRead,
            SystemPermissions.Catalog.GenresCreate,
            SystemPermissions.Catalog.GenresUpdate,
            SystemPermissions.Catalog.GenresDelete,
            SystemPermissions.Catalog.AssociationsManage,
            SystemPermissions.Catalog.AdminMediaRead,
            SystemPermissions.ShuffleBasic,
            SystemPermissions.ShuffleVip,
            SystemPermissions.AuthManage
        ];

        var seededCodes = permissions.Select(p => p.Code).ToHashSet();
        foreach (var expectedCode in expectedPermissions)
        {
            seededCodes.Should().Contain(expectedCode, $"Permission {expectedCode} should be seeded");
        }

        // Assert Roles and RolePermissions
        var roles = await context.Roles
            .Include(r => r.RolePermissions)
            .ToListAsync();

        var guestRole = roles.First(r => r.NormalizedName == SystemRoles.Guest.ToUpperInvariant());
        var standardRole = roles.First(r => r.NormalizedName == SystemRoles.Standard.ToUpperInvariant());
        var premiumRole = roles.First(r => r.NormalizedName == SystemRoles.Premium.ToUpperInvariant());
        var adminRole = roles.First(r => r.NormalizedName == SystemRoles.Admin.ToUpperInvariant());

        var permIdToCode = permissions.ToDictionary(p => p.Id, p => p.Code);

        // Guest role should have all catalog read permissions and basic shuffle
        var guestPermCodes = guestRole.RolePermissions.Select(rp => permIdToCode[rp.PermissionId]).ToList();
        guestPermCodes.Should().Contain([
            SystemPermissions.Catalog.MoviesRead,
            SystemPermissions.Catalog.SeriesRead,
            SystemPermissions.Catalog.EpisodesRead,
            SystemPermissions.Catalog.PlatformsRead,
            SystemPermissions.Catalog.MoodsRead,
            SystemPermissions.Catalog.GenresRead,
            SystemPermissions.ShuffleBasic
        ]);
        guestPermCodes.Should().NotContain(SystemPermissions.Catalog.MoviesCreate);

        // Standard role should have all catalog read permissions and basic shuffle
        var standardPermCodes = standardRole.RolePermissions.Select(rp => permIdToCode[rp.PermissionId]).ToList();
        standardPermCodes.Should().Contain(guestPermCodes);

        // Premium role should also have shuffle:vip
        var premiumPermCodes = premiumRole.RolePermissions.Select(rp => permIdToCode[rp.PermissionId]).ToList();
        premiumPermCodes.Should().Contain(SystemPermissions.ShuffleVip);

        // Admin role should have all permissions
        adminRole.RolePermissions.Count.Should().Be(permissions.Count, "Admin role must have every permission seeded");
    }

    [Fact]
    public async Task SeedAsync_WhenRunMultipleTimes_ShouldBeIdempotent()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var context = CreateInMemoryContext(dbName);

        // Act - Run 1
        await IdentityDbSeeder.SeedAsync(context);
        var permCount1 = await context.Permissions.CountAsync();
        var roleCount1 = await context.Roles.CountAsync();

        // Act - Run 2 (Idempotency check)
        await IdentityDbSeeder.SeedAsync(context);
        var permCount2 = await context.Permissions.CountAsync();
        var roleCount2 = await context.Roles.CountAsync();

        // Assert
        permCount2.Should().Be(permCount1);
        roleCount2.Should().Be(roleCount1);
    }
}
