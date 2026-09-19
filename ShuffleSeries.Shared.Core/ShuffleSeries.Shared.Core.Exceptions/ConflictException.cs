using System.Net;

namespace ShuffleSeries.Shared.Core.Exceptions;

public class ConflictException : CustomException
{
    public ConflictException(string message, string? code = "CONFLICT")
        : base(message, HttpStatusCode.Conflict, code, "Conflict")
    {
    }

    public ConflictException()
        : base("A conflict occurred with an existing resource.", HttpStatusCode.Conflict, "CONFLICT", "Conflict")
    {
    }
}