using ShuffleSeries.Shared.Core.Exceptions;

namespace ShuffleSeries.Identity.Domain.Exceptions;

public sealed class RefreshTokenExpiredException : UnauthorizedException
{
    private const string DefaultCode = "REFRESH_TOKEN_EXPIRED";

    public RefreshTokenExpiredException(string message = "Refresh token has expired.")
        : base(message, DefaultCode)
    {
    }
}
