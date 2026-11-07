using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace ShuffleSeries.Shared.Core.Application.Middlewares;

/// <summary>
/// Wolverine compile-time middleware that logs execution flow for all requests,
/// leveraging Serilog structured destructuring policies for automated, asynchronous PII masking.
/// </summary>
public static class LoggingMiddleware
{
    public static void Before(ILogger logger, Envelope envelope)
    {
        // No-op before handler execution
    }

    public static void After(ILogger logger, Envelope envelope)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            var message = envelope.Message;
            var requestName = message?.GetType().Name ?? "Unknown";

            logger.LogInformation(
                "Handled request {RequestName}. Request: {@RequestPayload}",
                requestName,
                message);
        }
    }

    public static void OnException(Exception ex, Envelope envelope, ILogger logger)
    {
        if (logger.IsEnabled(LogLevel.Warning))
        {
            var message = envelope.Message;
            var requestName = message?.GetType().Name ?? "Unknown";

#pragma warning disable S6667 // Intentionally logging request payload at Warning level without re-logging exception stack trace to avoid duplicate error logging with GlobalExceptionHandler (S2139)
            logger.LogWarning(
                "Request {RequestName} failed with error: {ErrorMessage}. Request: {@RequestPayload}",
                requestName,
                ex.Message,
                message);
#pragma warning restore S6667
        }

        // Rethrow preserving original stack trace so ASP.NET Core GlobalExceptionHandler or retry policies handle it
        ExceptionDispatchInfo.Capture(ex).Throw();
    }
}
