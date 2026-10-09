using ShuffleSeries.Shared.Core.Exceptions;

namespace ShuffleSeries.Identity.Domain.Exceptions;

public sealed class GuestUserNotFoundException : NotFoundException
{
    private const string DefaultCode = "GUEST_USER_NOT_FOUND";

    public GuestUserNotFoundException(string message = "Guest user not found or is already registered.")
        : base(message, DefaultCode)
    {
    }
}
