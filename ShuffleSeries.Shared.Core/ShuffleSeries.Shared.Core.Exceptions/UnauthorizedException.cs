using System.Net;

namespace ShuffleSeries.Shared.Core.Exceptions;

public class UnauthorizedException : CustomException
{
    public UnauthorizedException(string message = "You are not authorized to perform this action.",
        string? code = "UNAUTHORIZED")
        : base(message, HttpStatusCode.Unauthorized, code, "Unauthorized")
    {
    }
}