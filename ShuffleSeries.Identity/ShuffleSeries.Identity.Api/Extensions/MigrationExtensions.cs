using Microsoft.EntityFrameworkCore;
using ShuffleSeries.Identity.Domain.Entities;
using ShuffleSeries.Identity.Infrastructure.Persistence;
using ShuffleSeries.Shared.Core.Domain.Constants;

namespace ShuffleSeries.Identity.Api.Extensions;

public static class MigrationExtensions
{
    public static async Task ApplyMigrationsAsync(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        await using var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<IdentityDbContext>>();

        var retryCount = 0;
        const int maxRetries = 6;
        const int delaySeconds = 5;

        while (retryCount < maxRetries)
        {
            try
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Checking and applying Identity database migrations... (Attempt {Attempt}/{Max})",
                        retryCount + 1, maxRetries);
                }

                await context.Database.MigrateAsync();

                // Seed Default Roles and Permissions (Hybrid Role & Claim Structure)
                await SeedRolesAndPermissionsAsync(context, logger);

                logger.LogInformation("Identity database migrations and seeds applied successfully.");
                break;
            }
            catch (Exception ex)
            {
                retryCount++;
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning(ex,
                        "Identity database is not accepting connections yet. Retrying in {Delay} seconds...",
                        delaySeconds);
                }

                if (retryCount >= maxRetries)
                {
                    logger.LogError(ex,
                        "Maximum retry limit reached. Identity database migrations could not be applied.");
                    throw;
                }

                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            }
        }
    }

    private static async Task SeedRolesAndPermissionsAsync(IdentityDbContext context, ILogger logger)
    {
        // 1. Permissions Seed
        var permissions = new List<Permission>
        {
            Permission.Create(SystemPermissions.CatalogRead, "Catalog", "Allows reading movies, series and episodes"),
            Permission.Create(SystemPermissions.CatalogCreate, "Catalog", "Allows creating catalog content"),
            Permission.Create(SystemPermissions.CatalogUpdate, "Catalog", "Allows updating catalog content"),
            Permission.Create(SystemPermissions.CatalogDelete, "Catalog", "Allows deleting catalog content"),
            Permission.Create(SystemPermissions.ShuffleBasic, "Shuffle", "Allows basic random episode shuffle"),
            Permission.Create(SystemPermissions.ShuffleVip, "Shuffle", "Allows VIP curated weekend arena shuffle"),
            Permission.Create(SystemPermissions.AuthManage, "Identity", "Allows managing users and roles")
        };

        var existingPermissions = await context.Permissions.ToListAsync();
        foreach (var permission in permissions)
        {
            if (existingPermissions.All(p => p.Code != permission.Code))
            {
                context.Permissions.Add(permission);
                existingPermissions.Add(permission);
            }
        }

        await context.SaveChangesAsync();

        // 2. Roles Seed
        var guestRole = await context.Roles.Include(r => r.RolePermissions).FirstOrDefaultAsync(r => r.NormalizedName == SystemRoles.Guest.ToUpperInvariant());
        if (guestRole is null)
        {
            guestRole = Role.Create(SystemRoles.Guest, "Temporary unregistered guest session", isDefault: false);
            context.Roles.Add(guestRole);
        }

        var standardRole = await context.Roles.Include(r => r.RolePermissions).FirstOrDefaultAsync(r => r.NormalizedName == SystemRoles.Standard.ToUpperInvariant());
        if (standardRole is null)
        {
            standardRole = Role.Create(SystemRoles.Standard, "Registered standard platform user", isDefault: true);
            context.Roles.Add(standardRole);
        }

        var premiumRole = await context.Roles.Include(r => r.RolePermissions).FirstOrDefaultAsync(r => r.NormalizedName == SystemRoles.Premium.ToUpperInvariant());
        if (premiumRole is null)
        {
            premiumRole = Role.Create(SystemRoles.Premium, "Subscribed VIP platform user", isDefault: false);
            context.Roles.Add(premiumRole);
        }

        var adminRole = await context.Roles.Include(r => r.RolePermissions).FirstOrDefaultAsync(r => r.NormalizedName == SystemRoles.Admin.ToUpperInvariant());
        if (adminRole is null)
        {
            adminRole = Role.Create(SystemRoles.Admin, "Full platform administrative access", isDefault: false);
            context.Roles.Add(adminRole);
        }

        await context.SaveChangesAsync();

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

        await context.SaveChangesAsync();
        logger.LogInformation("Hybrid Roles and Claims successfully verified and seeded.");
    }

    private static void MapPermission(Role role, Dictionary<string, Guid> permMap, string code)
    {
        if (permMap.TryGetValue(code, out var permId))
        {
            role.AddPermission(permId);
        }
    }
}
