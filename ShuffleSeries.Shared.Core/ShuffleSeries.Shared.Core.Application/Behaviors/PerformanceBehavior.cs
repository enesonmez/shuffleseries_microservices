using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ShuffleSeries.Shared.Core.Application.Behaviors;

/// <summary>
/// Pipeline behavior that measures request execution duration.
/// Produces a structured warning log if execution time exceeds the performance threshold (default 500ms).
/// </summary>
public sealed class PerformanceBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IBaseRequest
{
    private const long ThresholdMilliseconds = 500;

    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;

    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var startTimestamp = Stopwatch.GetTimestamp();

        var response = await next(cancellationToken);

        var elapsedMilliseconds = (long)Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;

        if (elapsedMilliseconds > ThresholdMilliseconds && _logger.IsEnabled(LogLevel.Warning))
        {
            var requestName = typeof(TRequest).Name;

            _logger.LogWarning(
                "Long Running Request Detected: {RequestName} took {ElapsedMilliseconds} ms (Threshold: {Threshold} ms) with payload {@Payload}",
                requestName,
                elapsedMilliseconds,
                ThresholdMilliseconds,
                request);
        }

        return response;
    }
}
