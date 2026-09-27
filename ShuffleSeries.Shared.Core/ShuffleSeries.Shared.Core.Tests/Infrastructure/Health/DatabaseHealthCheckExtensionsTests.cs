using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using ShuffleSeries.Shared.Core.Infrastructure.Health;

namespace ShuffleSeries.Shared.Core.Tests.Infrastructure.Health;

public class DatabaseHealthCheckExtensionsTests
{
    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        {
        }
    }

    [Fact]
    public void AddSharedDatabaseHealthCheck_WithDefaultParameters_ShouldRegisterDbContextCheckWithDefaultTags()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase("TestDb_Defaults"));

        // Act
        services.AddSharedDatabaseHealthCheck<TestDbContext>();

        var serviceProvider = services.BuildServiceProvider();
        var healthCheckOptions = serviceProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;

        // Assert
        var registration = healthCheckOptions.Registrations.FirstOrDefault(r => r.Name == "PostgreSQL");
        registration.Should().NotBeNull();
        registration!.FailureStatus.Should().Be(HealthStatus.Unhealthy);
        registration.Tags.Should().Contain(["db", "postgresql", "ready"]);
    }

    [Fact]
    public void AddSharedDatabaseHealthCheck_WithCustomParameters_ShouldApplyCustomNameAndTags()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase("TestDb_Custom"));

        // Act
        services.AddSharedDatabaseHealthCheck<TestDbContext>(
            name: "CustomDatabase",
            failureStatus: HealthStatus.Degraded,
            customTags: ["custom-db", "custom-tag"]);

        var serviceProvider = services.BuildServiceProvider();
        var healthCheckOptions = serviceProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;

        // Assert
        var registration = healthCheckOptions.Registrations.FirstOrDefault(r => r.Name == "CustomDatabase");
        registration.Should().NotBeNull();
        registration!.FailureStatus.Should().Be(HealthStatus.Degraded);
        registration.Tags.Should().Contain(["custom-db", "custom-tag"]);
    }
}
