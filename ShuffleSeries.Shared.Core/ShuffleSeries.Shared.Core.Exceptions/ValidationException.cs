using System.Net;

namespace ShuffleSeries.Shared.Core.Exceptions;

public sealed class ValidationException : CustomException
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation failures have occurred.", HttpStatusCode.BadRequest, "VALIDATION_ERROR",
            "Validation Error")
    {
        Errors = errors;
    }

    public ValidationException(string propertyName, string errorMessage)
        : this(new Dictionary<string, string[]> { [propertyName] = [errorMessage] })
    {
    }

    public ValidationException()
        : this(new Dictionary<string, string[]>())
    {
    }
}