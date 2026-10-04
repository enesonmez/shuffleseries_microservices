using ShuffleSeries.Shared.Core.Exceptions;

namespace ShuffleSeries.Identity.Domain.Exceptions;

public sealed class InvalidExternalTokenException : UnauthorizedException
{
    private const string DefaultCode = "INVALID_EXTERNAL_TOKEN";

    public InvalidExternalTokenException(string provider)
        : base($"Invalid {provider} authentication token.", DefaultCode)
    {
    }
}
