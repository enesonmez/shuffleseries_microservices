using ShuffleSeries.Shared.Core.Exceptions;

namespace ShuffleSeries.Identity.Domain.Exceptions;

public sealed class TargetUserNotFoundException : NotFoundException
{
    private const string DefaultCode = "TARGET_USER_NOT_FOUND";

    public TargetUserNotFoundException(string message = "Target user account not found.")
        : base(message, DefaultCode)
    {
    }
}
