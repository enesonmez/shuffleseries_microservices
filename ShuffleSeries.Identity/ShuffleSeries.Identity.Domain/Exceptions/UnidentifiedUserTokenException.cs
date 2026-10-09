using ShuffleSeries.Shared.Core.Exceptions;

namespace ShuffleSeries.Identity.Domain.Exceptions;

public sealed class UnidentifiedUserTokenException : UnauthorizedException
{
    private const string DefaultCode = "UNAUTHORIZED";

    public UnidentifiedUserTokenException(string message = "User ID could not be identified from token.")
        : base(message, DefaultCode)
    {
    }
}
