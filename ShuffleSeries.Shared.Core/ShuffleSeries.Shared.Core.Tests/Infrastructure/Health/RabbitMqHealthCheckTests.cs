using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using RabbitMQ.Client;
using ShuffleSeries.Shared.Core.Infrastructure.Health;

namespace ShuffleSeries.Shared.Core.Tests.Infrastructure.Health;

public class RabbitMqHealthCheckTests
{
    private readonly Mock<IConnectionFactory> _factoryMock = new();
    private readonly Mock<IConnection> _connectionMock = new();
    private readonly Mock<IChannel> _channelMock = new();

    public RabbitMqHealthCheckTests()
    {
        _factoryMock
            .Setup(f => f.CreateConnectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_connectionMock.Object);

        _connectionMock
            .Setup(c => c.CreateChannelAsync(It.IsAny<CreateChannelOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_channelMock.Object);
    }

    [Fact]
    public void Constructor_WhenConnectionFactoryIsNull_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new RabbitMqHealthCheck((IConnectionFactory)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("connectionFactory");
    }

    [Fact]
    public void Constructor_WhenConfigurationIsNull_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new RabbitMqHealthCheck((IConfiguration)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("configuration");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenConnectionAndChannelAreOpen_ShouldReturnHealthyWithLatencyAndData()
    {
        // Arrange
        _connectionMock.Setup(c => c.IsOpen).Returns(true);
        _channelMock.Setup(c => c.IsOpen).Returns(true);

        var sut = new RabbitMqHealthCheck(_factoryMock.Object);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("RabbitMQ", sut, HealthStatus.Degraded, null)
        };

        // Act
        var result = await sut.CheckHealthAsync(context);

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("RabbitMQ broker bağlantısı sağlıklı");
        result.Data.Should().ContainKey("latencyMs");
        result.Data.Should().ContainKey("host");
        result.Data.Should().ContainKey("port");
        result.Data.Should().ContainKey("virtualHost");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenConnectionIsNotOpen_ShouldReturnFailureStatus()
    {
        // Arrange
        _connectionMock.Setup(c => c.IsOpen).Returns(false);

        var sut = new RabbitMqHealthCheck(_factoryMock.Object);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("RabbitMQ", sut, HealthStatus.Degraded, null)
        };

        // Act
        var result = await sut.CheckHealthAsync(context);

        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("RabbitMQ bağlantısı aktif değil");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenChannelIsNotOpen_ShouldReturnFailureStatus()
    {
        // Arrange
        _connectionMock.Setup(c => c.IsOpen).Returns(true);
        _channelMock.Setup(c => c.IsOpen).Returns(false);

        var sut = new RabbitMqHealthCheck(_factoryMock.Object);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("RabbitMQ", sut, HealthStatus.Degraded, null)
        };

        // Act
        var result = await sut.CheckHealthAsync(context);

        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("RabbitMQ kanalı oluşturulamadı veya aktif değil");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenExceptionThrown_ShouldReturnFailureStatusWithException()
    {
        // Arrange
        var expectedException = new InvalidOperationException("RabbitMQ connection refused");
        _factoryMock
            .Setup(f => f.CreateConnectionAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        var sut = new RabbitMqHealthCheck(_factoryMock.Object);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("RabbitMQ", sut, HealthStatus.Degraded, null)
        };

        // Act
        var result = await sut.CheckHealthAsync(context);

        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("sağlık kontrolü sırasında hata oluştu");
        result.Exception.Should().BeSameAs(expectedException);
    }
}
