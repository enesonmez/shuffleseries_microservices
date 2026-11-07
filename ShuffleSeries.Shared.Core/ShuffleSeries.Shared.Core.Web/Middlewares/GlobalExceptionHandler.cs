using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ShuffleSeries.Shared.Core.Exceptions;
using ShuffleSeries.Shared.Core.Web.Helpers;

namespace ShuffleSeries.Shared.Core.Web.Middlewares;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (httpContext.Response.HasStarted)
        {
            _logger.LogWarning("The response has already started, the global exception handler cannot handle this exception.");
            return false;
        }

        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            _logger.LogDebug("Request was canceled by the client: {Path}. TraceId: {TraceId}", httpContext.Request.Path, traceId);
            httpContext.Response.StatusCode = 499; // Client Closed Request
            return true;
        }

        var correlationContext = httpContext.RequestServices?.GetService(typeof(Correlation.ICorrelationIdContext))
            as Correlation.ICorrelationIdContext;
        var correlationId = correlationContext?.CorrelationId ?? "N/A";

        if (exception is BadHttpRequestException)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning("Bad HTTP request: {Message}. TraceId: {TraceId}", exception.Message, traceId);
            }
        }
        else if (_logger.IsEnabled(LogLevel.Error))
        {
            _logger.LogError(exception, "An unhandled exception occurred: {Message}. CorrelationId: {CorrelationId}, TraceId: {TraceId}",
                exception.Message, correlationId, traceId);
        }

        int status;
        string title;
        string detail;
        string code;
        IReadOnlyDictionary<string, string[]>? errors = null;

        switch (exception)
        {
            case FluentValidation.ValidationException fluentValidationException:
                status = StatusCodes.Status400BadRequest;
                title = "Validation Error";
                detail = "One or more validation errors occurred.";
                code = "VALIDATION_ERROR";
                errors = fluentValidationException.Errors
                    .Where(f => f is not null)
                    .GroupBy(f => f.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).Distinct().ToArray());
                break;

            case CustomException customException:
                status = (int)customException.StatusCode;
                title = customException.Title ?? GetDefaultTitleForStatus(status);
                detail = customException.Message;
                code = !string.IsNullOrWhiteSpace(customException.Code) ? customException.Code : "CUSTOM_ERROR";
                break;

            case BadHttpRequestException badHttpRequestException:
                status = badHttpRequestException.StatusCode;
                title = GetDefaultTitleForStatus(status);
                detail = "The request payload or parameters are invalid or malformed.";
                code = "BAD_REQUEST";
                break;

            default:
                status = StatusCodes.Status500InternalServerError;
                title = "Internal Server Error";
                detail = "An unexpected error occurred on the server.";
                code = "INTERNAL_SERVER_ERROR";
                break;
        }

        await ProblemDetailsWriter.WriteAsync(
            httpContext,
            status,
            title,
            detail,
            code,
            GetRfcTypeForStatus(status),
            configure: problem =>
            {
                if (errors is not null)
                {
                    problem.Extensions["errors"] = errors;
                }
            },
            cancellationToken: cancellationToken);

        return true;
    }

    private static string GetRfcTypeForStatus(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        StatusCodes.Status401Unauthorized => "https://tools.ietf.org/html/rfc7235#section-3.1",
        StatusCodes.Status403Forbidden => "https://tools.ietf.org/html/rfc7231#section-6.5.3",
        StatusCodes.Status404NotFound => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
        StatusCodes.Status409Conflict => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
        StatusCodes.Status422UnprocessableEntity => "https://tools.ietf.org/html/rfc4918#section-11.2",
        _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1"
    };

    private static string GetDefaultTitleForStatus(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "Bad Request",
        StatusCodes.Status401Unauthorized => "Unauthorized",
        StatusCodes.Status403Forbidden => "Forbidden",
        StatusCodes.Status404NotFound => "Not Found",
        StatusCodes.Status409Conflict => "Conflict",
        StatusCodes.Status422UnprocessableEntity => "Business Rule Violation",
        _ => "Internal Server Error"
    };
}
