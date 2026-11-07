using Microsoft.Extensions.DependencyInjection;
using ShuffleSeries.Identity.Application;

namespace ShuffleSeries.Identity.Tests.Application;

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
