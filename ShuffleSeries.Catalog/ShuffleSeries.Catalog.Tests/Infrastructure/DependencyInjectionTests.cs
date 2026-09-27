using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Catalog.Domain.Services;
using ShuffleSeries.Catalog.Infrastructure;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Catalog.Tests.Infrastructure;

public class DependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_ShouldRegisterCatalogServices()
    {
        var services = new ServiceCollection();
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Database"] = "Host=localhost;Database=testdb;Username=postgres;Password=postgres;",
            ["MessageBroker:Host"] = "localhost",
            ["MessageBroker:Username"] = "guest",
            ["MessageBroker:Password"] = "guest"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        services.AddInfrastructure(configuration);

        services.Should().Contain(d => d.ServiceType == typeof(ISeriesRepository));
        services.Should().Contain(d => d.ServiceType == typeof(IUnitOfWork));
        services.Should().Contain(d => d.ServiceType == typeof(SeriesDomainService));
    }
}
