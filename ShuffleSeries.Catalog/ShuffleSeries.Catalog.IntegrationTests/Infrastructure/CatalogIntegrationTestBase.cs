using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using ShuffleSeries.Catalog.Infrastructure.BackgroundJobs;
using ShuffleSeries.Catalog.Infrastructure.Persistence;
using ShuffleSeries.Shared.Core.Domain.Constants;

namespace ShuffleSeries.Catalog.IntegrationTests.Infrastructure;

[Collection(CatalogCollectionFixture.Name)]
public abstract class CatalogIntegrationTestBase : IAsyncLifetime
{
    protected CatalogIntegrationTestBase(CatalogApiFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
        var defaultToken = TestJwtGenerator.GenerateToken();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", defaultToken);
    }

    protected CatalogApiFactory Factory { get; }
    protected HttpClient Client { get; }

    protected HttpClient CreateUnauthorizedClient()
    {
        return Factory.CreateClient();
    }

    protected HttpClient CreateAuthorizedClient(
        string role = SystemRoles.Admin,
        IEnumerable<string>? permissions = null)
    {
        var client = Factory.CreateClient();
        var token = TestJwtGenerator.GenerateToken(role: role, permissions: permissions);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    // Her test metodundan önce veritabanı şemasını silmeden tabloları temizle, Redis'i flush'la ve seed verisini tazele
    public virtual async Task InitializeAsync()
    {
        await Factory.ResetStateAsync();
        await EnsureCatalogDataSeededAsync();
    }

    public virtual Task DisposeAsync() => Task.CompletedTask;

    protected async Task TriggerOutboxJobAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<ProcessOutboxMessagesJob>();
        await job.Execute(null!);
    }

    protected async Task EnsureCatalogDataSeededAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        await CatalogDbSeeder.SeedAsync(context);
    }

    protected async Task ExecuteDbContextAsync(Func<CatalogDbContext, Task> action)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        await action(context);
    }

    protected async Task<T> ExecuteDbContextAsync<T>(Func<CatalogDbContext, Task<T>> action)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        return await action(context);
    }
}
