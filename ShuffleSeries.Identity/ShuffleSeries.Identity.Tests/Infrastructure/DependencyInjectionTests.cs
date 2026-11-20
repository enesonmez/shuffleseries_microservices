using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShuffleSeries.Identity.Application.Common.Interfaces;
using ShuffleSeries.Identity.Domain.Repositories;
using ShuffleSeries.Identity.Infrastructure;
using ShuffleSeries.Shared.Core.Domain.Repositories;

namespace ShuffleSeries.Identity.Tests.Infrastructure;

public class DependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_ShouldRegisterIdentityServices()
    {
        var services = new ServiceCollection();
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Database"] = "Host=localhost;Database=testdb;Username=postgres;Password=postgres;",
            ["MessageBroker:Host"] = "localhost",
            ["MessageBroker:Username"] = "guest",
            ["MessageBroker:Password"] = "guest",
            ["Redis:ConnectionString"] = "localhost:6379",
            ["Jwt:Secret"] = "super-secret-key-that-is-at-least-32-chars-long",
            ["Jwt:Issuer"] = "ShuffleSeries",
            ["Jwt:Audience"] = "ShuffleSeries"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        services.AddInfrastructure(configuration);

        services.Should().Contain(d => d.ServiceType == typeof(IUserRepository));
        services.Should().Contain(d => d.ServiceType == typeof(IRoleRepository));
        services.Should().Contain(d => d.ServiceType == typeof(IPermissionRepository));
        services.Should().Contain(d => d.ServiceType == typeof(IUnitOfWork));
        services.Should().Contain(d => d.ServiceType == typeof(ITokenService));
        services.Should().Contain(d => d.ServiceType == typeof(IPasswordHasher));
        services.Should().Contain(d => d.ServiceType == typeof(IPermissionResolver));
        services.Should().Contain(d => d.ServiceType == typeof(IExternalAuthService));
    }
}
