using System.Net;

namespace ShuffleSeries.Shared.Core.Exceptions;

public class ForbiddenException : CustomException
{
    public ForbiddenException(string message = "You do not have permission to access this resource.",
        string? code = "FORBIDDEN")
        : base(message, HttpStatusCode.Forbidden, code, "Forbidden")
    {
    }
}