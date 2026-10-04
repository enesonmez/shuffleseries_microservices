using ShuffleSeries.Shared.Core.Exceptions;

namespace ShuffleSeries.Identity.Domain.Exceptions;

public sealed class InvalidRefreshTokenException : UnauthorizedException
{
    private const string DefaultCode = "INVALID_REFRESH_TOKEN";

    public InvalidRefreshTokenException(string message = "Invalid refresh token.")
        : base(message, DefaultCode)
    {
    }
}
