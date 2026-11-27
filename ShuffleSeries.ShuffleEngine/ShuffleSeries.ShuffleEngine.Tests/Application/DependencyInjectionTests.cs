using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ShuffleSeries.ShuffleEngine.Application;

namespace ShuffleSeries.ShuffleEngine.Tests.Application;

public class DependencyInjectionTests
{
    [Fact]
    public void AddApplication_ShouldRegisterValidatorsAndApplicationServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddApplication();

        services.Any(s => typeof(IValidator).IsAssignableFrom(s.ServiceType)).Should().BeTrue();
    }
}
