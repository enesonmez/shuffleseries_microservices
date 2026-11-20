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
    public async Task CheckHealthAsync_WhenCalledMultipleTimesWhileConnectionIsOpen_ShouldReuseConnectionWithoutSocketChurn()
    {
        // Arrange
        _connectionMock.Setup(c => c.IsOpen).Returns(true);
        _channelMock.Setup(c => c.IsOpen).Returns(true);

        await using var sut = new RabbitMqHealthCheck(_factoryMock.Object);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("RabbitMQ", sut, HealthStatus.Degraded, null)
        };

        // Act
        var firstResult = await sut.CheckHealthAsync(context);
        var secondResult = await sut.CheckHealthAsync(context);

        // Assert
        firstResult.Status.Should().Be(HealthStatus.Healthy);
        secondResult.Status.Should().Be(HealthStatus.Healthy);
        _factoryMock.Verify(f => f.CreateConnectionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _connectionMock.Verify(
            c => c.CreateChannelAsync(It.IsAny<CreateChannelOptions>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
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

    [Fact]
    public void Constructor_WithEmptyConfiguration_ShouldFallbackToDefaultsAndDisposeCleanly()
    {
        var configuration = new ConfigurationBuilder().Build();

        using var sut = new RabbitMqHealthCheck(configuration);

        sut.Should().NotBeNull();
    }

    [Fact]
    public async Task Dispose_WhenConnectionWasOpened_ShouldDisposeUnderlyingConnection()
    {
        _connectionMock.Setup(c => c.IsOpen).Returns(true);
        _channelMock.Setup(c => c.IsOpen).Returns(true);

        var sut = new RabbitMqHealthCheck(_factoryMock.Object);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("RabbitMQ", sut, HealthStatus.Degraded, null)
        };

        await sut.CheckHealthAsync(context);

        sut.Dispose();

        _connectionMock.Verify(c => c.Dispose(), Times.Once);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenCachedConnectionCloses_ShouldDisposeStaleAndReconnect()
    {
        var connectionIsOpen = true;
        _connectionMock.Setup(c => c.IsOpen).Returns(() => connectionIsOpen);
        _channelMock.Setup(c => c.IsOpen).Returns(true);
        _factoryMock
            .Setup(f => f.CreateConnectionAsync(It.IsAny<CancellationToken>()))
            .Callback(() => connectionIsOpen = true)
            .ReturnsAsync(_connectionMock.Object);

        await using var sut = new RabbitMqHealthCheck(_factoryMock.Object);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("RabbitMQ", sut, HealthStatus.Degraded, null)
        };

        var firstResult = await sut.CheckHealthAsync(context);
        connectionIsOpen = false;
        var secondResult = await sut.CheckHealthAsync(context);

        firstResult.Status.Should().Be(HealthStatus.Healthy);
        secondResult.Status.Should().Be(HealthStatus.Healthy);
        _factoryMock.Verify(f => f.CreateConnectionAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        _connectionMock.Verify(c => c.DisposeAsync(), Times.AtLeastOnce);
    }
}
