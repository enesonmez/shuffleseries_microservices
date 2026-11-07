using Microsoft.Extensions.Logging;
using Moq;
using ShuffleSeries.Shared.Core.Application.Middlewares;
using Wolverine;

namespace ShuffleSeries.Shared.Core.Tests.Application.Middlewares;

public class LoggingMiddlewareTests
{
    private record SampleCommand(string SensitivePassword, string Email);

    [Fact]
    public void After_WhenRequestSucceeds_ShouldLogInformationWithRequest()
    {
        var mockLogger = new Mock<ILogger>();
        mockLogger.Setup(l => l.IsEnabled(LogLevel.Information)).Returns(true);

        var command = new SampleCommand("Secret123", "user@shuffleseries.com");
        var envelope = new Envelope(command);

        LoggingMiddleware.After(mockLogger.Object, envelope);

        mockLogger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Handled request SampleCommand")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void After_WhenInformationDisabled_ShouldNotLog()
    {
        var mockLogger = new Mock<ILogger>();
        mockLogger.Setup(l => l.IsEnabled(LogLevel.Information)).Returns(false);

        var command = new SampleCommand("Secret123", "user@shuffleseries.com");
        var envelope = new Envelope(command);

        LoggingMiddleware.After(mockLogger.Object, envelope);

        mockLogger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public void OnException_WhenExceptionOccurs_ShouldLogWarningAndRethrow()
    {
        var mockLogger = new Mock<ILogger>();
        mockLogger.Setup(l => l.IsEnabled(LogLevel.Warning)).Returns(true);

        var command = new SampleCommand("Secret123", "user@shuffleseries.com");
        var ex = new InvalidOperationException("Database connection timeout");
        var envelope = new Envelope(command);

        var act = () => LoggingMiddleware.OnException(ex, envelope, mockLogger.Object);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Database connection timeout");

        mockLogger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString()!.Contains("Request SampleCommand failed with error: Database connection timeout")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void OnException_WhenWarningDisabled_ShouldStillRethrowWithoutLogging()
    {
        var mockLogger = new Mock<ILogger>();
        mockLogger.Setup(l => l.IsEnabled(LogLevel.Warning)).Returns(false);

        var command = new SampleCommand("Secret123", "user@shuffleseries.com");
        var ex = new InvalidOperationException("Database connection timeout");
        var envelope = new Envelope(command);

        var act = () => LoggingMiddleware.OnException(ex, envelope, mockLogger.Object);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Database connection timeout");

        mockLogger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }
}
