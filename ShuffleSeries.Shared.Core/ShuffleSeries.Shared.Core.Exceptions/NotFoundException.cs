using System.Net;

namespace ShuffleSeries.Shared.Core.Exceptions;

public class NotFoundException : CustomException
{
    public NotFoundException(string message, string? code = "NOT_FOUND")
        : base(message, HttpStatusCode.NotFound, code, "Not Found")
    {
    }

    public NotFoundException(string entityName, object key)
        : base($"Entity '{entityName}' with identifier '{key}' was not found.", HttpStatusCode.NotFound,
            $"{entityName.ToUpperInvariant()}_NOT_FOUND", "Not Found")
    {
    }

    public NotFoundException()
        : base("The requested resource was not found.", HttpStatusCode.NotFound, "NOT_FOUND", "Not Found")
    {
    }
}
