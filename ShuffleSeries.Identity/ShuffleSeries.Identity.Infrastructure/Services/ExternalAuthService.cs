using ShuffleSeries.Identity.Application.Interfaces;
using ShuffleSeries.Identity.Application.Models;

namespace ShuffleSeries.Identity.Infrastructure.Services;

/// <summary>
/// Composite dispatcher for external social authentication.
/// Follows Open/Closed Principle (OCP) by resolving registered ISocialAuthProvider implementations.
/// </summary>
internal sealed class ExternalAuthService : IExternalAuthService
{
    private readonly IEnumerable<ISocialAuthProvider> _providers;

    public ExternalAuthService(IEnumerable<ISocialAuthProvider> providers)
    {
        _providers = providers;
    }

    public async Task<ExternalUserPrincipal?> VerifyTokenAsync(
        string provider,
        string idToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(idToken))
        {
            return null;
        }

        var authProvider = _providers.FirstOrDefault(p =>
            p.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase));

        if (authProvider is null)
        {
            return null;
        }

        return await authProvider.ValidateTokenAsync(idToken, cancellationToken);
    }
}
