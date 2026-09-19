using System.Net;

namespace ShuffleSeries.Shared.Core.Exceptions;

public abstract class CustomException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public string? Code { get; protected set; }
    public string? Title { get; }

    protected CustomException(
        string message,
        HttpStatusCode statusCode = HttpStatusCode.InternalServerError,
        string? code = null,
        string? title = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        Code = code;
        Title = title;
    }
}