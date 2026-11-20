using Microsoft.Extensions.Logging;
using ShuffleSeries.Shared.Core.Application.Middlewares;
using Wolverine;

namespace ShuffleSeries.Shared.Core.Tests.Application.Middlewares;

public class LoggingMiddlewareTests
{
    private record SampleCommand(string SensitivePassword, string Email);

    private sealed class CapturingLogger(LogLevel? enabledLevel = null) : ILogger
    {
        public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => enabledLevel == logLevel;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception), exception));
    }

    [Fact]
    public void After_WhenRequestSucceeds_ShouldLogInformationWithRequest()
    {
        var logger = new CapturingLogger(LogLevel.Information);
        var command = new SampleCommand("Secret123", "user@shuffleseries.com");
        var envelope = new Envelope(command);

        LoggingMiddleware.After(logger, envelope);

        logger.Entries.Should().ContainSingle(e =>
            e.Level == LogLevel.Information &&
            e.Message.Contains("Handled request SampleCommand"));
    }

    [Fact]
    public void After_WhenInformationDisabled_ShouldNotLog()
    {
        var logger = new CapturingLogger();
        var command = new SampleCommand("Secret123", "user@shuffleseries.com");
        var envelope = new Envelope(command);

        LoggingMiddleware.After(logger, envelope);

        logger.Entries.Should().BeEmpty();
    }

    [Fact]
    public void OnException_WhenExceptionOccurs_ShouldLogWarningAndRethrow()
    {
        var logger = new CapturingLogger(LogLevel.Warning);
        var command = new SampleCommand("Secret123", "user@shuffleseries.com");
        var ex = new InvalidOperationException("Database connection timeout");
        var envelope = new Envelope(command);

        var act = () => LoggingMiddleware.OnException(ex, envelope, logger);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Database connection timeout");

        logger.Entries.Should().ContainSingle(e =>
            e.Level == LogLevel.Warning &&
            e.Message.Contains("Request SampleCommand failed with error: Database connection timeout"));
    }

    [Fact]
    public void OnException_WhenValidationExceptionOccurs_ShouldSanitizeErrorMessageToPreventRecordToStringPiiLeak()
    {
        var logger = new CapturingLogger(LogLevel.Warning);
        var command = new SampleCommand("SuperSecretPlaintextPassword123!", "user@shuffleseries.com");
        var ex = new FluentValidation.ValidationException(
            $"Validation failure on: {command}",
            [new FluentValidation.Results.ValidationFailure("Email", "Invalid email")]);
        var envelope = new Envelope(command);

        var act = () => LoggingMiddleware.OnException(ex, envelope, logger);

        act.Should().Throw<FluentValidation.ValidationException>();

        logger.Entries.Should().ContainSingle(e =>
            e.Level == LogLevel.Warning &&
            e.Message.Contains("One or more validation errors occurred.") &&
            !e.Message.Contains("Validation failure on:"));
    }

    [Fact]
    public void OnException_WhenWarningDisabled_ShouldStillRethrowWithoutLogging()
    {
        var logger = new CapturingLogger();
        var command = new SampleCommand("Secret123", "user@shuffleseries.com");
        var ex = new InvalidOperationException("Database connection timeout");
        var envelope = new Envelope(command);

        var act = () => LoggingMiddleware.OnException(ex, envelope, logger);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Database connection timeout");

        logger.Entries.Should().BeEmpty();
    }
}
