using Microsoft.EntityFrameworkCore;
using ShuffleSeries.Identity.Domain.Entities;
using ShuffleSeries.Shared.Core.Domain.Constants;

namespace ShuffleSeries.Identity.Infrastructure.Persistence;

public static class IdentityDbSeeder
{
    private const string CatalogModule = "Catalog";

    public static async Task SeedAsync(IdentityDbContext context, CancellationToken cancellationToken = default)
    {
        // 1. Permissions Seed
        var permissions = new List<Permission>
        {
            Permission.Create(SystemPermissions.CatalogRead, CatalogModule, "Allows reading movies, series and episodes"),
            Permission.Create(SystemPermissions.CatalogCreate, CatalogModule, "Allows creating catalog content"),
            Permission.Create(SystemPermissions.CatalogUpdate, CatalogModule, "Allows updating catalog content"),
            Permission.Create(SystemPermissions.CatalogDelete, CatalogModule, "Allows deleting catalog content"),
            Permission.Create(SystemPermissions.ShuffleBasic, "Shuffle", "Allows basic random episode shuffle"),
            Permission.Create(SystemPermissions.ShuffleVip, "Shuffle", "Allows VIP curated weekend arena shuffle"),
            Permission.Create(SystemPermissions.AuthManage, "Identity", "Allows managing users and roles")
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

        // Guest: catalog:read, shuffle:basic
        MapPermission(guestRole, permMap, SystemPermissions.CatalogRead);
        MapPermission(guestRole, permMap, SystemPermissions.ShuffleBasic);

        // Standard: catalog:read, shuffle:basic
        MapPermission(standardRole, permMap, SystemPermissions.CatalogRead);
        MapPermission(standardRole, permMap, SystemPermissions.ShuffleBasic);

        // Premium: catalog:read, shuffle:basic, shuffle:vip
        MapPermission(premiumRole, permMap, SystemPermissions.CatalogRead);
        MapPermission(premiumRole, permMap, SystemPermissions.ShuffleBasic);
        MapPermission(premiumRole, permMap, SystemPermissions.ShuffleVip);

        // Admin: all
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
