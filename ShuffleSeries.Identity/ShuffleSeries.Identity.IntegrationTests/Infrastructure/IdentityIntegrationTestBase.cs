using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShuffleSeries.Identity.Domain.Entities;
using ShuffleSeries.Identity.Infrastructure.BackgroundJobs;
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
        await Factory.ResetStateAsync();
        await EnsureRolesAndPermissionsSeededAsync();
    }

    public virtual Task DisposeAsync() => Task.CompletedTask;

    protected async Task TriggerOutboxJobAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<ProcessOutboxMessagesJob>();
        await job.Execute(null!);
    }

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
                Permission.Create(SystemPermissions.Catalog.MoviesRead, "Catalog", "Read movies"),
                Permission.Create(SystemPermissions.Catalog.MoviesCreate, "Catalog", "Create movies"),
                Permission.Create(SystemPermissions.ShuffleBasic, "Shuffle", "Basic shuffle"),
                Permission.Create(SystemPermissions.ShuffleVip, "Shuffle", "VIP shuffle")
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
            standardRole.AddPermission(permMap[SystemPermissions.Catalog.MoviesRead]);
            standardRole.AddPermission(permMap[SystemPermissions.ShuffleBasic]);

            guestRole.AddPermission(permMap[SystemPermissions.Catalog.MoviesRead]);
            guestRole.AddPermission(permMap[SystemPermissions.ShuffleBasic]);

            premiumRole.AddPermission(permMap[SystemPermissions.Catalog.MoviesRead]);
            premiumRole.AddPermission(permMap[SystemPermissions.ShuffleBasic]);
            premiumRole.AddPermission(permMap[SystemPermissions.ShuffleVip]);

            foreach (var p in permissions)
            {
                adminRole.AddPermission(p.Id);
            }

            await context.SaveChangesAsync();
        });
    }
}
