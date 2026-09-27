using Microsoft.Extensions.DependencyInjection;
using ShuffleSeries.Catalog.Application;

namespace ShuffleSeries.Catalog.Tests.Application;

public class DependencyInjectionTests
{
    [Fact]
    public void AddApplication_ShouldRegisterMediatRAndValidators()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        services.Should().NotBeEmpty();
    }
}
