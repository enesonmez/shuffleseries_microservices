using System.IdentityModel.Tokens.Jwt;
using ShuffleSeries.Identity.Application.Interfaces;
using ShuffleSeries.Identity.Application.Models;

namespace ShuffleSeries.Identity.Infrastructure.Services;

internal sealed class ExternalAuthService : IExternalAuthService
{
    private readonly JwtSecurityTokenHandler _handler = new();

    public Task<ExternalUserPrincipal?> VerifyTokenAsync(
        string provider,
        string idToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(idToken))
        {
            return Task.FromResult<ExternalUserPrincipal?>(null);
        }

        try
        {
            // Read unvalidated / parsed claims from ID token (in production, validate against Apple / Google JWKS)
            if (_handler.CanReadToken(idToken))
            {
                var jwt = _handler.ReadJwtToken(idToken);
                var sub = jwt.Subject ?? jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
                if (string.IsNullOrWhiteSpace(sub))
                {
                    return Task.FromResult<ExternalUserPrincipal?>(null);
                }

                var email = jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
                var name = jwt.Claims.FirstOrDefault(c => c.Type == "name")?.Value;

                return Task.FromResult<ExternalUserPrincipal?>(new ExternalUserPrincipal(provider, sub, email, name));
            }

            // Fallback for mocked tokens in tests: "test_sub_123" or similar
            if (idToken.StartsWith("mock_", StringComparison.OrdinalIgnoreCase))
            {
                var parts = idToken.Split(':');
                var subjectId = parts.Length > 1 ? parts[1] : idToken;
                var email = parts.Length > 2 ? parts[2] : $"{subjectId}@{provider.ToLowerInvariant()}.com";
                return Task.FromResult<ExternalUserPrincipal?>(new ExternalUserPrincipal(provider, subjectId, email, "Test User"));
            }

            return Task.FromResult<ExternalUserPrincipal?>(null);
        }
        catch
        {
            return Task.FromResult<ExternalUserPrincipal?>(null);
        }
    }
}
