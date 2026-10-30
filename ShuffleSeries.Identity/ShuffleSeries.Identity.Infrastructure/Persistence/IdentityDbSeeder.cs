using Microsoft.EntityFrameworkCore;
using ShuffleSeries.Identity.Domain.Entities;
using ShuffleSeries.Shared.Core.Domain.Constants;

namespace ShuffleSeries.Identity.Infrastructure.Persistence;

public static class IdentityDbSeeder
{
    private const string CatalogModule = "Catalog";
    private const string ShuffleModule = "Shuffle";
    private const string IdentityModule = "Identity";

    public static async Task SeedAsync(IdentityDbContext context, CancellationToken cancellationToken = default)
    {
        // 1. Permissions Seed
        var permissions = new List<Permission>
        {
            // Movies Granular Permissions
            Permission.Create(SystemPermissions.Catalog.MoviesRead, CatalogModule, "Allows viewing movie details"),
            Permission.Create(SystemPermissions.Catalog.MoviesCreate, CatalogModule, "Allows creating new movies in catalog"),
            Permission.Create(SystemPermissions.Catalog.MoviesUpdate, CatalogModule, "Allows updating movie metadata"),
            Permission.Create(SystemPermissions.Catalog.MoviesDelete, CatalogModule, "Allows soft-deleting movies"),

            // Series Granular Permissions
            Permission.Create(SystemPermissions.Catalog.SeriesRead, CatalogModule, "Allows viewing series and season details"),
            Permission.Create(SystemPermissions.Catalog.SeriesCreate, CatalogModule, "Allows creating new series in catalog"),
            Permission.Create(SystemPermissions.Catalog.SeriesUpdate, CatalogModule, "Allows updating series metadata"),
            Permission.Create(SystemPermissions.Catalog.SeriesDelete, CatalogModule, "Allows soft-deleting series"),

            // Episodes Granular Permissions
            Permission.Create(SystemPermissions.Catalog.EpisodesRead, CatalogModule, "Allows viewing episode details"),
            Permission.Create(SystemPermissions.Catalog.EpisodesCreate, CatalogModule, "Allows creating new episodes under series"),
            Permission.Create(SystemPermissions.Catalog.EpisodesUpdate, CatalogModule, "Allows updating episode metadata"),
            Permission.Create(SystemPermissions.Catalog.EpisodesDelete, CatalogModule, "Allows soft-deleting episodes"),

            // Platforms Granular Permissions
            Permission.Create(SystemPermissions.Catalog.PlatformsRead, CatalogModule, "Allows reading streaming platforms list"),
            Permission.Create(SystemPermissions.Catalog.PlatformsCreate, CatalogModule, "Allows creating new streaming platforms"),
            Permission.Create(SystemPermissions.Catalog.PlatformsUpdate, CatalogModule, "Allows updating platform details and logos"),
            Permission.Create(SystemPermissions.Catalog.PlatformsDelete, CatalogModule, "Allows soft-deleting streaming platforms"),

            // Moods Granular Permissions
            Permission.Create(SystemPermissions.Catalog.MoodsRead, CatalogModule, "Allows reading catalog moods list"),
            Permission.Create(SystemPermissions.Catalog.MoodsCreate, CatalogModule, "Allows creating new mood categories"),
            Permission.Create(SystemPermissions.Catalog.MoodsUpdate, CatalogModule, "Allows updating mood metadata and ambient colors"),
            Permission.Create(SystemPermissions.Catalog.MoodsDelete, CatalogModule, "Allows soft-deleting mood categories"),

            // Genres Granular Permissions
            Permission.Create(SystemPermissions.Catalog.GenresRead, CatalogModule, "Allows reading catalog genres taxonomy"),
            Permission.Create(SystemPermissions.Catalog.GenresCreate, CatalogModule, "Allows creating new genre categories"),
            Permission.Create(SystemPermissions.Catalog.GenresUpdate, CatalogModule, "Allows updating genre metadata"),
            Permission.Create(SystemPermissions.Catalog.GenresDelete, CatalogModule, "Allows soft-deleting genre categories"),

            // Media & Episode Associations & Translations
            Permission.Create(SystemPermissions.Catalog.AssociationsManage, CatalogModule, "Allows managing media and episode platform, mood, genre associations and translations"),

            // Admin CMS
            Permission.Create(SystemPermissions.Catalog.AdminMediaRead, CatalogModule, "Allows access to admin CMS media management listing"),

            // Shuffle Permissions
            Permission.Create(SystemPermissions.ShuffleBasic, ShuffleModule, "Allows basic random episode shuffle"),
            Permission.Create(SystemPermissions.ShuffleVip, ShuffleModule, "Allows VIP curated weekend arena shuffle"),

            // Identity Permissions
            Permission.Create(SystemPermissions.AuthManage, IdentityModule, "Allows managing users and roles")
        };

        var existingPermissions = await context.Permissions
            .IgnoreQueryFilters()
            .ToListAsync(cancellationToken);
        var missingPermissions = permissions
            .Where(permission => existingPermissions.All(p => p.Code != permission.Code))
            .ToList();

        foreach (var permission in missingPermissions)
        {
            permission.ClearDomainEvents();
            context.Permissions.Add(permission);
            existingPermissions.Add(permission);
        }

        await context.SaveChangesAsync(cancellationToken);

        // 2. Roles Seed
        var guestNormalized = SystemRoles.Guest.ToUpperInvariant();
        var guestRole = await context.Roles
            .IgnoreQueryFilters()
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.NormalizedName == guestNormalized, cancellationToken);
        if (guestRole is null)
        {
            guestRole = Role.Create(SystemRoles.Guest, "Temporary unregistered guest session", isDefault: false);
            guestRole.ClearDomainEvents();
            context.Roles.Add(guestRole);
        }

        var standardNormalized = SystemRoles.Standard.ToUpperInvariant();
        var standardRole = await context.Roles
            .IgnoreQueryFilters()
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.NormalizedName == standardNormalized, cancellationToken);
        if (standardRole is null)
        {
            standardRole = Role.Create(SystemRoles.Standard, "Registered standard platform user", isDefault: true);
            standardRole.ClearDomainEvents();
            context.Roles.Add(standardRole);
        }

        var premiumNormalized = SystemRoles.Premium.ToUpperInvariant();
        var premiumRole = await context.Roles
            .IgnoreQueryFilters()
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.NormalizedName == premiumNormalized, cancellationToken);
        if (premiumRole is null)
        {
            premiumRole = Role.Create(SystemRoles.Premium, "Subscribed VIP platform user", isDefault: false);
            premiumRole.ClearDomainEvents();
            context.Roles.Add(premiumRole);
        }

        var adminNormalized = SystemRoles.Admin.ToUpperInvariant();
        var adminRole = await context.Roles
            .IgnoreQueryFilters()
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.NormalizedName == adminNormalized, cancellationToken);
        if (adminRole is null)
        {
            adminRole = Role.Create(SystemRoles.Admin, "Full platform administrative access", isDefault: false);
            adminRole.ClearDomainEvents();
            context.Roles.Add(adminRole);
        }

        await context.SaveChangesAsync(cancellationToken);

        // 3. Role-Permission Mappings
        var permMap = existingPermissions.ToDictionary(p => p.Code, p => p.Id);

        // Granular catalog reading permissions for all authenticated/guest users
        string[] allCatalogReadPermissions =
        [
            SystemPermissions.Catalog.MoviesRead,
            SystemPermissions.Catalog.SeriesRead,
            SystemPermissions.Catalog.EpisodesRead,
            SystemPermissions.Catalog.PlatformsRead,
            SystemPermissions.Catalog.MoodsRead,
            SystemPermissions.Catalog.GenresRead
        ];

        // Guest: All catalog read + shuffle:basic
        foreach (var readPerm in allCatalogReadPermissions)
        {
            MapPermission(guestRole, permMap, readPerm);
        }
        MapPermission(guestRole, permMap, SystemPermissions.ShuffleBasic);

        // Standard: All catalog read + shuffle:basic
        foreach (var readPerm in allCatalogReadPermissions)
        {
            MapPermission(standardRole, permMap, readPerm);
        }
        MapPermission(standardRole, permMap, SystemPermissions.ShuffleBasic);

        // Premium: All catalog read + shuffle:basic + shuffle:vip
        foreach (var readPerm in allCatalogReadPermissions)
        {
            MapPermission(premiumRole, permMap, readPerm);
        }
        MapPermission(premiumRole, permMap, SystemPermissions.ShuffleBasic);
        MapPermission(premiumRole, permMap, SystemPermissions.ShuffleVip);

        // Admin: all permissions in the system
        foreach (var perm in existingPermissions)
        {
            adminRole.AddPermission(perm.Id);
        }

        guestRole.ClearDomainEvents();
        standardRole.ClearDomainEvents();
        premiumRole.ClearDomainEvents();
        adminRole.ClearDomainEvents();

        await context.SaveChangesAsync(cancellationToken);
    }

    private static void MapPermission(Role role, Dictionary<string, Guid> permMap, string code)
    {
        if (permMap.TryGetValue(code, out var permId))
        {
            role.AddPermission(permId);
        }
    }
}
