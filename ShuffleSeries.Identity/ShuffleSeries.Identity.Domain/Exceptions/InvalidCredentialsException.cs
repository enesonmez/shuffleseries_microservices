using ShuffleSeries.Shared.Core.Exceptions;

namespace ShuffleSeries.Identity.Domain.Exceptions;

public sealed class InvalidCredentialsException : UnauthorizedException
{
    private const string DefaultCode = "INVALID_CREDENTIALS";

    public InvalidCredentialsException(string message = "Invalid email or password.")
        : base(message, DefaultCode)
    {
    }
}
