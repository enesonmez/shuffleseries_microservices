using ShuffleSeries.Shared.Core.Exceptions;

namespace ShuffleSeries.Identity.Domain.Exceptions;

public sealed class TokenNotFoundException : NotFoundException
{
    private const string DefaultCode = "TOKEN_NOT_FOUND";

    public TokenNotFoundException(string message = "Token not found.")
        : base(message, DefaultCode)
    {
    }
}
