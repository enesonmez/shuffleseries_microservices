using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShuffleSeries.ShuffleEngine.Application.Common.Interfaces;
using ShuffleSeries.ShuffleEngine.Infrastructure;
using ShuffleSeries.ShuffleEngine.Infrastructure.Persistence;

namespace ShuffleSeries.ShuffleEngine.Tests.Infrastructure;

public class DependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_ShouldRegister_DbContextAndShuffleStores()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Database"] = "Host=localhost;Port=5432;Database=shuffleseries_shuffle;Username=postgres;Password=postgres",
                ["CatalogGrpc:Address"] = "http://localhost:5012"
            })
            .Build();

        services.AddInfrastructure(configuration);

        services.Any(s => s.ServiceType == typeof(ShuffleEngineDbContext)).Should().BeTrue();
        services.Any(s => s.ServiceType == typeof(IShufflePoolStore)).Should().BeTrue();
        services.Any(s => s.ServiceType == typeof(IUserShuffleStateStore)).Should().BeTrue();
        services.Any(s => s.ServiceType == typeof(ICatalogSyncGrpcClient)).Should().BeTrue();
        services.Any(s => s.ServiceType == typeof(IShuffleCacheWarmingService)).Should().BeTrue();
        services.Any(s => s.ServiceType == typeof(IInboxIdempotencyService)).Should().BeTrue();
    }
}
