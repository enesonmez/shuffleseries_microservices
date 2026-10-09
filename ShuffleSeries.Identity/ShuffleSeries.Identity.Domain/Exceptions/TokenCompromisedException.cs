using ShuffleSeries.Shared.Core.Exceptions;

namespace ShuffleSeries.Identity.Domain.Exceptions;

public sealed class TokenCompromisedException : UnauthorizedException
{
    private const string DefaultCode = "TOKEN_REUSE_DETECTED";

    public TokenCompromisedException(string message = "Compromised token detected. All active sessions have been revoked.")
        : base(message, DefaultCode)
    {
    }
}
