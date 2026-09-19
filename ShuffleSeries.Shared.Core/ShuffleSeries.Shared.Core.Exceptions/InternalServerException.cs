using System.Net;

namespace ShuffleSeries.Shared.Core.Exceptions;

public class InternalServerException : CustomException
{
    public InternalServerException(
        string message = "An internal server error occurred.",
        string? code = "INTERNAL_SERVER_ERROR",
        Exception? innerException = null)
        : base(message, HttpStatusCode.InternalServerError, code, "Internal Server Error", innerException)
    {
    }
}