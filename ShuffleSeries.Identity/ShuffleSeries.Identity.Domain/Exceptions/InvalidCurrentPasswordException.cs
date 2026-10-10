using ShuffleSeries.Shared.Core.Exceptions;

namespace ShuffleSeries.Identity.Domain.Exceptions;

public sealed class InvalidCurrentPasswordException : UnauthorizedException
{
    private const string DefaultCode = "INVALID_CURRENT_PASSWORD";

    public InvalidCurrentPasswordException(string message = "Current password is incorrect.")
        : base(message, DefaultCode)
    {
    }
}
