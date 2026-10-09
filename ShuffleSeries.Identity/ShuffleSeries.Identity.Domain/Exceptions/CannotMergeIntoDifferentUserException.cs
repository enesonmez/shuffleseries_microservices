using ShuffleSeries.Shared.Core.Exceptions;

namespace ShuffleSeries.Identity.Domain.Exceptions;

public sealed class CannotMergeIntoDifferentUserException : ForbiddenException
{
    private const string DefaultCode = "FORBIDDEN";

    public CannotMergeIntoDifferentUserException(string message = "Cannot merge guest account into another user's account.")
        : base(message, DefaultCode)
    {
    }
}
