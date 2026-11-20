using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Moq;
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

    [Theory]
    [InlineData("Production", true)]
    [InlineData("Development", false)]
    public async Task ReadinessEndpoint_ShouldSanitizeBasedOnEnvironment(string environmentName, bool shouldBeSanitized)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddRouting();
        builder.Services.AddHealthChecks()
            .AddCheck("FailingService", () => HealthCheckResult.Unhealthy(
                "Connection failed",
                new InvalidOperationException("Sensitive internal DB detail"),
                new Dictionary<string, object> { ["host"] = "10.0.0.5:5432" }));

        var mockEnv = new Mock<IHostEnvironment>();
        mockEnv.Setup(e => e.EnvironmentName).Returns(environmentName);
        builder.Services.AddSingleton(mockEnv.Object);

        var app = builder.Build();
        app.MapSharedHealthChecks();

        var readyEndpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(ds => ds.Endpoints)
            .OfType<RouteEndpoint>()
            .First(e => e.RoutePattern.RawText?.Contains("health/ready") == true);

        var context = new DefaultHttpContext
        {
            RequestServices = app.Services
        };
        var stream = new MemoryStream();
        context.Response.Body = stream;

        await readyEndpoint.RequestDelegate!(context);

        stream.Seek(0, SeekOrigin.Begin);
        var json = await new StreamReader(stream).ReadToEndAsync();
        using var doc = JsonDocument.Parse(json);
        var checks = doc.RootElement.GetProperty("checks").EnumerateArray().ToList();
        var check = checks.First();

        if (shouldBeSanitized)
        {
            check.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Null);
            check.GetProperty("error").ValueKind.Should().Be(JsonValueKind.Null);
        }
        else
        {
            check.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Object);
            check.GetProperty("error").GetString().Should().Be("Sensitive internal DB detail");
        }
    }
}
