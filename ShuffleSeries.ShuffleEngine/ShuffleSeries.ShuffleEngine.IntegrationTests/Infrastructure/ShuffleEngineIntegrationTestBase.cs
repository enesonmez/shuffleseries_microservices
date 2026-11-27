using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using ShuffleSeries.ShuffleEngine.Infrastructure.Persistence;

namespace ShuffleSeries.ShuffleEngine.IntegrationTests.Infrastructure;

[Collection(ShuffleEngineCollectionFixture.Name)]
public abstract class ShuffleEngineIntegrationTestBase : IAsyncLifetime
{
    protected ShuffleEngineIntegrationTestBase(ShuffleEngineApiFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
        var defaultToken = TestJwtGenerator.GenerateToken();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", defaultToken);
    }

    protected ShuffleEngineApiFactory Factory { get; }

    protected HttpClient Client { get; }

    protected HttpClient CreateUnauthorizedClient() => Factory.CreateClient();

    protected HttpClient CreateAuthorizedClient(
        string role = SystemRoles.Admin,
        IEnumerable<string>? permissions = null)
    {
        var client = Factory.CreateClient();
        var token = TestJwtGenerator.GenerateToken(role: role, permissions: permissions);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public virtual async Task InitializeAsync()
    {
        await Factory.ResetStateAsync();
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ShuffleEngineDbContext>();
        await ShuffleEngineDbSeeder.SeedAsync(context);
    }

    public virtual Task DisposeAsync() => Task.CompletedTask;
}
