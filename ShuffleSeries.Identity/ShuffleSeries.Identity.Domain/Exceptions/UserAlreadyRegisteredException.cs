using ShuffleSeries.Shared.Core.Exceptions;

namespace ShuffleSeries.Identity.Domain.Exceptions;

public sealed class UserAlreadyRegisteredException : BusinessException
{
    private const string DefaultCode = "USER_ALREADY_REGISTERED";

    public UserAlreadyRegisteredException(string message = "User is already a registered user.")
        : base(DefaultCode, message)
    {
    }
}
