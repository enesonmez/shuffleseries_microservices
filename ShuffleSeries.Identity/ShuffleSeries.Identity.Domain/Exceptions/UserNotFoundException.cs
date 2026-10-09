using ShuffleSeries.Shared.Core.Exceptions;

namespace ShuffleSeries.Identity.Domain.Exceptions;

public sealed class UserNotFoundException : NotFoundException
{
    private const string DefaultCode = "USER_NOT_FOUND";

    public UserNotFoundException(string message = "User not found.")
        : base(message, DefaultCode)
    {
    }

    public UserNotFoundException(Guid userId)
        : base($"User with ID '{userId}' was not found.", DefaultCode)
    {
    }
}
