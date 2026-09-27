using Microsoft.Extensions.DependencyInjection;
using ShuffleSeries.Shared.Core.Infrastructure;
using ShuffleSeries.Shared.Core.Infrastructure.Interceptors;

namespace ShuffleSeries.Shared.Core.Tests.Infrastructure;

public class DependencyInjectionTests
{
    [Fact]
    public void AddSharedInfrastructure_ShouldRegisterInterceptors()
    {
        var services = new ServiceCollection();

        services.AddSharedInfrastructure();

        services.Should().Contain(d => d.ServiceType == typeof(InsertOutboxMessagesInterceptor));
        services.Should().Contain(d => d.ServiceType == typeof(SoftDeleteInterceptor));
    }
}
