using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using ShuffleSeries.Shared.Core.Application.Middlewares;
using Wolverine;

namespace ShuffleSeries.Shared.Core.Tests.Application.Middlewares;

public class PerformanceMiddlewareTests
{
    private record SampleCommand(string Data);

    [Fact]
    public void Before_ShouldReturnValidTimestamp()
    {
        var timestamp = PerformanceMiddleware.Before();
        timestamp.Should().BeGreaterThan(0);
    }

    [Fact]
    public void After_WhenExecutionIsFast_ShouldNotLogWarning()
    {
        var mockLogger = new Mock<ILogger>();
        mockLogger.Setup(l => l.IsEnabled(LogLevel.Warning)).Returns(true);

        var startTimestamp = Stopwatch.GetTimestamp();
        var command = new SampleCommand("Fast");
        var envelope = new Envelope(command);

        PerformanceMiddleware.After(startTimestamp, mockLogger.Object, envelope);

        mockLogger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public void After_WhenExecutionIsSlow_ShouldLogWarningWithPayload()
    {
        var mockLogger = new Mock<ILogger>();
        mockLogger.Setup(l => l.IsEnabled(LogLevel.Warning)).Returns(true);

        // Simulate timestamp from 600 ms in the past
        var fakePastTimestamp = Stopwatch.GetTimestamp() - (long)(Stopwatch.Frequency * 0.6);
        var command = new SampleCommand("SlowData");
        var envelope = new Envelope(command);

        PerformanceMiddleware.After(fakePastTimestamp, mockLogger.Object, envelope);

        mockLogger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Long Running Request Detected: SampleCommand")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void After_WhenWarningDisabled_ShouldNotLogEvenIfSlow()
    {
        var mockLogger = new Mock<ILogger>();
        mockLogger.Setup(l => l.IsEnabled(LogLevel.Warning)).Returns(false);

        var fakePastTimestamp = Stopwatch.GetTimestamp() - (long)(Stopwatch.Frequency * 0.6);
        var command = new SampleCommand("SlowData");
        var envelope = new Envelope(command);

        PerformanceMiddleware.After(fakePastTimestamp, mockLogger.Object, envelope);

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
