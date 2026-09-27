using MediatR;
using Microsoft.Extensions.Logging;

namespace ShuffleSeries.Shared.Core.Application.Behaviors;

/// <summary>
/// Pipeline behavior that logs execution flow for all MediatR requests and responses,
/// leveraging Serilog structured destructuring policies for automated, asynchronous PII masking.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IBaseRequest
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        try
        {
            var response = await next(cancellationToken);

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "Handled request {RequestName}. Request: {@RequestPayload}, Response: {@ResponsePayload}",
                    requestName,
                    request,
                    response);
            }

            return response;
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
#pragma warning disable S6667 // Intentionally logging request payload at Warning level without re-logging exception stack trace to avoid duplicate error logging with GlobalExceptionHandler (S2139)
                _logger.LogWarning(
                    "Request {RequestName} failed with error: {ErrorMessage}. Request: {@RequestPayload}",
                    requestName,
                    ex.Message,
                    request);
#pragma warning restore S6667
            }

            throw;
        }
    }
}
