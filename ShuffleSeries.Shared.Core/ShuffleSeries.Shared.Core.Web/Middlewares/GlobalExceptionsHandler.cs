using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ShuffleSeries.Shared.Core.Exceptions;

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

        _logger.LogError(exception, "An unhandled exception occurred: {Message}. TraceId: {TraceId}",
            exception.Message, traceId);

        var problemDetails = new ProblemDetails
        {
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["traceId"] = traceId;

        switch (exception)
        {
            case ValidationException validationException:
                problemDetails.Status = (int)validationException.StatusCode;
                problemDetails.Type = GetRfcTypeForStatus(problemDetails.Status.Value);
                problemDetails.Title = validationException.Title ?? "Validation Error";
                problemDetails.Detail = validationException.Message;
                if (!string.IsNullOrWhiteSpace(validationException.Code))
                    problemDetails.Extensions["code"] = validationException.Code;
                problemDetails.Extensions["errors"] = validationException.Errors;
                break;

            case CustomException customException:
                problemDetails.Status = (int)customException.StatusCode;
                problemDetails.Type = GetRfcTypeForStatus(problemDetails.Status.Value);
                problemDetails.Title = customException.Title ?? GetDefaultTitleForStatus(problemDetails.Status.Value);
                problemDetails.Detail = customException.Message;
                if (!string.IsNullOrWhiteSpace(customException.Code))
                    problemDetails.Extensions["code"] = customException.Code;
                break;

            case ArgumentException argumentException:
                problemDetails.Status = StatusCodes.Status400BadRequest;
                problemDetails.Type = GetRfcTypeForStatus(StatusCodes.Status400BadRequest);
                problemDetails.Title = "Bad Request";
                problemDetails.Detail = argumentException.Message;
                problemDetails.Extensions["code"] = "INVALID_ARGUMENT";
                break;

            default:
                problemDetails.Status = StatusCodes.Status500InternalServerError;
                problemDetails.Type = GetRfcTypeForStatus(StatusCodes.Status500InternalServerError);
                problemDetails.Title = "Internal Server Error";
                problemDetails.Detail = "An unexpected error occurred on the server.";
                problemDetails.Extensions["code"] = "INTERNAL_SERVER_ERROR";
                break;
        }

        httpContext.Response.StatusCode = problemDetails.Status.Value;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, options: null,
            contentType: "application/problem+json", cancellationToken: cancellationToken);

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