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
        var errorMessage = ex is FluentValidation.ValidationException
            ? "One or more validation errors occurred."
            : ex.Message;

        LogRequestFailure(logger, envelope, errorMessage);

        // Rethrow preserving original stack trace so ASP.NET Core GlobalExceptionHandler or retry policies handle it
        ExceptionDispatchInfo.Capture(ex).Throw();
    }

    private static void LogRequestFailure(ILogger logger, Envelope envelope, string errorMessage)
    {
        if (!logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        var message = envelope.Message;
        var requestName = message?.GetType().Name ?? "Unknown";

        logger.LogWarning(
            "Request {RequestName} failed with error: {ErrorMessage}. Request: {@RequestPayload}",
            requestName,
            errorMessage,
            message);
    }
}
