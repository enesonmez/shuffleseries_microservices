using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Moq;
using ShuffleSeries.Shared.Core.Infrastructure.Health;
using StackExchange.Redis;

namespace ShuffleSeries.Shared.Core.Tests.Infrastructure.Health;

public class RedisHealthCheckTests
{
    private readonly Mock<IConnectionMultiplexer> _redisMock = new();
    private readonly Mock<IDatabase> _databaseMock = new();

    public RedisHealthCheckTests()
    {
        _redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_databaseMock.Object);
    }

    [Fact]
    public void Constructor_WhenRedisIsNull_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new RedisHealthCheck(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("redis");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenConnectedAndPingSucceeds_ShouldReturnHealthyWithLatencyAndEndpoints()
    {
        // Arrange
        _redisMock.Setup(r => r.IsConnected).Returns(true);
        _redisMock.Setup(r => r.GetEndPoints(It.IsAny<bool>()))
            .Returns([new DnsEndPoint("localhost", 6379)]);
        _databaseMock.Setup(db => db.PingAsync(It.IsAny<CommandFlags>()))
            .ReturnsAsync(TimeSpan.FromMilliseconds(2.5));

        var sut = new RedisHealthCheck(_redisMock.Object);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("Redis", sut, HealthStatus.Degraded, null)
        };

        // Act
        var result = await sut.CheckHealthAsync(context);

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("2.5ms");
        result.Data.Should().ContainKey("latencyMs");
        result.Data["latencyMs"].Should().Be(2.5);
        result.Data.Should().ContainKey("endpoints");
        result.Data["endpoints"].ToString().Should().Contain("localhost:6379");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenNotConnected_ShouldReturnFailureStatus()
    {
        // Arrange
        _redisMock.Setup(r => r.IsConnected).Returns(false);

        var sut = new RedisHealthCheck(_redisMock.Object);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("Redis", sut, HealthStatus.Degraded, null)
        };

        // Act
        var result = await sut.CheckHealthAsync(context);

        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("Redis bağlantısı aktif değil");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenPingThrowsException_ShouldReturnFailureStatusWithException()
    {
        // Arrange
        _redisMock.Setup(r => r.IsConnected).Returns(true);
        var expectedException = new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection lost");
        _databaseMock.Setup(db => db.PingAsync(It.IsAny<CommandFlags>()))
            .ThrowsAsync(expectedException);

        var sut = new RedisHealthCheck(_redisMock.Object);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("Redis", sut, HealthStatus.Degraded, null)
        };

        // Act
        var result = await sut.CheckHealthAsync(context);

        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("Redis sağlık kontrolü sırasında hata oluştu");
        result.Exception.Should().BeSameAs(expectedException);
    }

    [Fact]
    public void AddSharedRedisHealthCheck_WithDefaultParameters_ShouldRegisterRedisCheckWithDefaultTagsAndDegraded()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_redisMock.Object);

        // Act
        services.AddSharedRedisHealthCheck();

        var serviceProvider = services.BuildServiceProvider();
        var healthCheckOptions = serviceProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;

        // Assert
        var registration = healthCheckOptions.Registrations.FirstOrDefault(r => r.Name == "Redis");
        registration.Should().NotBeNull();
        registration!.FailureStatus.Should().Be(HealthStatus.Degraded);
        registration.Tags.Should().Contain(["cache", "redis", "ready"]);
    }

    [Fact]
    public void AddSharedRedisHealthCheck_WithCustomParameters_ShouldApplyCustomNameTagsAndFailureStatus()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_redisMock.Object);

        // Act
        services.AddSharedRedisHealthCheck(
            name: "CustomRedis",
            failureStatus: HealthStatus.Unhealthy,
            customTags: ["custom-cache", "live"]);

        var serviceProvider = services.BuildServiceProvider();
        var healthCheckOptions = serviceProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;

        // Assert
        var registration = healthCheckOptions.Registrations.FirstOrDefault(r => r.Name == "CustomRedis");
        registration.Should().NotBeNull();
        registration!.FailureStatus.Should().Be(HealthStatus.Unhealthy);
        registration.Tags.Should().Contain(["custom-cache", "live"]);
    }
}
