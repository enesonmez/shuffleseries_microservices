using Microsoft.Extensions.DependencyInjection;
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

    protected async Task EnsureRolesAndPermissionsSeededAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await IdentityDbSeeder.SeedAsync(context);
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
}
