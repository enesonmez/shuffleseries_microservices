using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using ShuffleSeries.Shared.Core.Infrastructure.Health;

namespace ShuffleSeries.Shared.Core.Tests.Infrastructure.Health;

public class RabbitMqHealthCheckExtensionsTests
{
    [Fact]
    public void AddSharedRabbitMqHealthCheck_WithDefaultParameters_ShouldRegisterRabbitMqCheckWithDefaultTagsAndDegraded()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(configuration);

        // Act
        services.AddSharedRabbitMqHealthCheck();

        var serviceProvider = services.BuildServiceProvider();
        var healthCheckOptions = serviceProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;

        // Assert
        var registration = healthCheckOptions.Registrations.FirstOrDefault(r => r.Name == "RabbitMQ");
        registration.Should().NotBeNull();
        registration!.FailureStatus.Should().Be(HealthStatus.Degraded);
        registration.Tags.Should().Contain(["messaging", "rabbitmq", "ready"]);
    }

    [Fact]
    public void AddSharedRabbitMqHealthCheck_WithCustomParameters_ShouldApplyCustomNameTagsAndFailureStatus()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(configuration);

        // Act
        services.AddSharedRabbitMqHealthCheck(
            name: "CustomRabbit",
            failureStatus: HealthStatus.Unhealthy,
            customTags: ["custom-bus", "live"]);

        var serviceProvider = services.BuildServiceProvider();
        var healthCheckOptions = serviceProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;

        // Assert
        var registration = healthCheckOptions.Registrations.FirstOrDefault(r => r.Name == "CustomRabbit");
        registration.Should().NotBeNull();
        registration!.FailureStatus.Should().Be(HealthStatus.Unhealthy);
        registration.Tags.Should().Contain(["custom-bus", "live"]);
    }
}
