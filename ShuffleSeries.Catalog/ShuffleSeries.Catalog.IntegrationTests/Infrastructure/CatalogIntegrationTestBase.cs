using Microsoft.Extensions.DependencyInjection;
using ShuffleSeries.Catalog.Infrastructure.Persistence;

namespace ShuffleSeries.Catalog.IntegrationTests.Infrastructure;

[Collection(CatalogCollectionFixture.Name)]
public abstract class CatalogIntegrationTestBase : IAsyncLifetime
{
    protected CatalogIntegrationTestBase(CatalogApiFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    protected CatalogApiFactory Factory { get; }
    protected HttpClient Client { get; }

    // Her test metodundan önce veritabanı şemasını silmeden tabloları temizle
    public virtual Task InitializeAsync() => Factory.ResetDatabaseAsync();

    public virtual Task DisposeAsync() => Task.CompletedTask;

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
