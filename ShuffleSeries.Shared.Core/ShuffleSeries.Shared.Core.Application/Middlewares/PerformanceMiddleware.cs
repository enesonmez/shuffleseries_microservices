using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace ShuffleSeries.Shared.Core.Application.Middlewares;

/// <summary>
/// Wolverine compile-time middleware that measures request execution duration.
/// Produces a structured warning log if execution time exceeds the performance threshold (default 500ms).
/// </summary>
public static class PerformanceMiddleware
{
    public const long ThresholdMilliseconds = 500;

    public static long Before() => Stopwatch.GetTimestamp();

    public static void After(long startTimestamp, ILogger logger, Envelope envelope)
    {
        var elapsedMilliseconds = (long)Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;

        if (elapsedMilliseconds > ThresholdMilliseconds && logger.IsEnabled(LogLevel.Warning))
        {
            var message = envelope.Message;
            var requestName = message?.GetType().Name ?? "Unknown";

            logger.LogWarning(
                "Long Running Request Detected: {RequestName} took {ElapsedMilliseconds} ms (Threshold: {Threshold} ms) with payload {@Payload}",
                requestName,
                elapsedMilliseconds,
                ThresholdMilliseconds,
                message);
        }
    }
}
