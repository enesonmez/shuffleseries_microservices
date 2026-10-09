using ShuffleSeries.Shared.Core.Exceptions;

namespace ShuffleSeries.Identity.Domain.Exceptions;

public sealed class EmailAlreadyInUseException : ConflictException
{
    private const string DefaultCode = "USER_ALREADY_EXISTS";

    public EmailAlreadyInUseException(string email)
        : base($"A user with email '{email}' already exists.", DefaultCode)
    {
    }
}
