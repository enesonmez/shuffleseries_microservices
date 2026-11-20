using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ShuffleSeries.Shared.Core.Application.Middlewares;
using Wolverine;

namespace ShuffleSeries.Shared.Core.Tests.Application.Middlewares;

public class PerformanceMiddlewareTests
{
    private record SampleCommand(string Data);

    private sealed class CapturingLogger(bool warningEnabled) : ILogger
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => warningEnabled && logLevel == LogLevel.Warning;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception)));
    }

    [Fact]
    public void Before_ShouldReturnValidTimestamp()
    {
        var timestamp = PerformanceMiddleware.Before();
        timestamp.Should().BeGreaterThan(0);
    }

    [Fact]
    public void After_WhenExecutionIsFast_ShouldNotLogWarning()
    {
        var logger = new CapturingLogger(warningEnabled: true);
        var startTimestamp = Stopwatch.GetTimestamp();
        var command = new SampleCommand("Fast");
        var envelope = new Envelope(command);

        PerformanceMiddleware.After(startTimestamp, logger, envelope);

        logger.Entries.Should().BeEmpty();
    }

    [Fact]
    public void After_WhenExecutionIsSlow_ShouldLogWarningWithPayload()
    {
        var logger = new CapturingLogger(warningEnabled: true);

        // Simulate timestamp from 600 ms in the past
        var fakePastTimestamp = Stopwatch.GetTimestamp() - (long)(Stopwatch.Frequency * 0.6);
        var command = new SampleCommand("SlowData");
        var envelope = new Envelope(command);

        PerformanceMiddleware.After(fakePastTimestamp, logger, envelope);

        logger.Entries.Should().ContainSingle(e =>
            e.Level == LogLevel.Warning &&
            e.Message.Contains("Long Running Request Detected: SampleCommand"));
    }

    [Fact]
    public void After_WhenWarningDisabled_ShouldNotLogEvenIfSlow()
    {
        var logger = new CapturingLogger(warningEnabled: false);
        var fakePastTimestamp = Stopwatch.GetTimestamp() - (long)(Stopwatch.Frequency * 0.6);
        var command = new SampleCommand("SlowData");
        var envelope = new Envelope(command);

        PerformanceMiddleware.After(fakePastTimestamp, logger, envelope);

        logger.Entries.Should().BeEmpty();
    }
}
