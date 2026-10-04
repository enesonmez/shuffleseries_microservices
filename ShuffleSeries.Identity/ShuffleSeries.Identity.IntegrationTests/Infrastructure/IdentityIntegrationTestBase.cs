using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShuffleSeries.Identity.Domain.Entities;
using ShuffleSeries.Identity.Infrastructure.Persistence;

namespace ShuffleSeries.Identity.IntegrationTests.Infrastructure;

[Collection(IdentityCollectionFixture.Name)]
public abstract class IdentityIntegrationTestBase : IAsyncLifetime
{
    protected IdentityIntegrationTestBase(IdentityApiFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    protected IdentityApiFactory Factory { get; }
    protected HttpClient Client { get; }

    public virtual async Task InitializeAsync()
    {
        await Factory.ResetDatabaseAsync();
        await EnsureRolesAndPermissionsSeededAsync();
    }

    public virtual Task DisposeAsync() => Task.CompletedTask;

    protected async Task ExecuteDbContextAsync(Func<IdentityDbContext, Task> action)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await action(context);
    }

    protected async Task<T> ExecuteDbContextAsync<T>(Func<IdentityDbContext, Task<T>> action)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        return await action(context);
    }

    private async Task EnsureRolesAndPermissionsSeededAsync()
    {
        await ExecuteDbContextAsync(async context =>
        {
            if (await context.Roles.AnyAsync())
            {
                return;
            }

            var permissions = new List<Permission>
            {
                Permission.Create("catalog:read", "Catalog", "Read catalog"),
                Permission.Create("catalog:create", "Catalog", "Create catalog"),
                Permission.Create("shuffle:basic", "Shuffle", "Basic shuffle"),
                Permission.Create("shuffle:vip", "Shuffle", "VIP shuffle")
            };
            context.Permissions.AddRange(permissions);
            await context.SaveChangesAsync();

            var standardRole = Role.Create("Standard", "Standard User", isDefault: true);
            var guestRole = Role.Create("Guest", "Guest User", isDefault: false);
            var premiumRole = Role.Create("Premium", "Premium User", isDefault: false);
            var adminRole = Role.Create("Admin", "Admin User", isDefault: false);

            context.Roles.AddRange(standardRole, guestRole, premiumRole, adminRole);
            await context.SaveChangesAsync();

            var permMap = permissions.ToDictionary(p => p.Code, p => p.Id);
            standardRole.AddPermission(permMap["catalog:read"]);
            standardRole.AddPermission(permMap["shuffle:basic"]);

            guestRole.AddPermission(permMap["catalog:read"]);
            guestRole.AddPermission(permMap["shuffle:basic"]);

            premiumRole.AddPermission(permMap["catalog:read"]);
            premiumRole.AddPermission(permMap["shuffle:basic"]);
            premiumRole.AddPermission(permMap["shuffle:vip"]);

            foreach (var p in permissions)
            {
                adminRole.AddPermission(p.Id);
            }

            await context.SaveChangesAsync();
        });
    }
}
