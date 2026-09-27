using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ShuffleSeries.Shared.Core.Web.Health;

namespace ShuffleSeries.Shared.Core.Tests.Web.Health;

public class HealthCheckExtensionsTests
{
    [Fact]
    public void AddSharedHealthChecks_ShouldRegisterHealthCheckServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddSharedHealthChecks();
        var provider = services.BuildServiceProvider();

        // Assert
        var healthCheckService = provider.GetService<HealthCheckService>();
        healthCheckService.Should().NotBeNull();
    }

    [Fact]
    public void MapSharedHealthChecks_ShouldMapLiveAndReadyEndpoints()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSharedHealthChecks();
        var app = builder.Build();

        // Act
        app.MapSharedHealthChecks();

        // Assert
        var endpoints = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(ds => ds.Endpoints)
            .OfType<RouteEndpoint>()
            .ToList();

        endpoints.Should().Contain(e => e.RoutePattern.RawText == "/health/live" || e.RoutePattern.RawText == "health/live");
        endpoints.Should().Contain(e => e.RoutePattern.RawText == "/health/ready" || e.RoutePattern.RawText == "health/ready");
    }
}
